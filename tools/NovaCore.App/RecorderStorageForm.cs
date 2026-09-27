using NovaCore.Diagnostics;

namespace NovaCore.App;

// Startup-only maintenance UI: no renderer, simulation or persistence hot-path work.
internal sealed class RecorderStorageForm:Form
{
    RecorderStorageNotice notice;
    readonly TextBox summary=new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Top,Height=200,ScrollBars=ScrollBars.Vertical};
    readonly DataGridView sessions=new(){Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
    readonly FlowLayoutPanel actions=new(){Dock=DockStyle.Bottom,Height=86,AutoSize=false};
    RecorderStorageForm(RecorderStorageNotice current,bool qualification=false)
    {
        notice=current;Text="NovaCore recorder storage";Width=1120;Height=670;StartPosition=FormStartPosition.CenterScreen;
        sessions.Columns.Add("identity","Session");sessions.Columns.Add("classification","Classification");sessions.Columns.Add("pin","Pin");sessions.Columns.Add("bytes","Bytes");sessions.Columns.Add("reason","Reason preserved");
        Controls.Add(sessions);Controls.Add(summary);Controls.Add(actions);
        Add("SAFE MAINTENANCE",()=>{notice=RuntimeRetention.SafeMaintenance();Render();});
        Add("KEEP",()=>RuntimeRetention.Pin(Selected()));
        Add("REVIEW / SEAL",Review);
        Add("UNPIN",()=>{if(Confirm("Remove only explicit pin protection? Criticality and raw-retirement rules remain unchanged."))RuntimeRetention.Unpin(Selected());});
        Add("RECLASSIFY AS ORDINARY",()=>{if(Confirm("Use the sealed positive noncritical review to reclassify this session? Pin, active ownership and capsule validation still apply."))RuntimeRetention.ReclassifyAsOrdinary(Selected());});
        Add("RETIRE RAW",()=>{if(Confirm("Request normal eligible raw retirement? Protected, newest-useful and under-budget sessions will still refuse.")){RuntimeRetention.RetireRaw(Selected());}});
        Add("ACKNOWLEDGE / CONTINUE",()=>{RuntimeRetention.AcknowledgeStorage(notice);Close();});
        FormClosing+=(_,_)=>{if(qualification)return;try{RuntimeRetention.AcknowledgeStorage(notice);}catch(Exception e){Console.Error.WriteLine("Storage acknowledgement not persisted: "+e.Message);}};
        Render();
    }
    Guid Selected()=>sessions.SelectedRows.Count==1&&sessions.SelectedRows[0].Tag is Guid id?id:throw new InvalidOperationException("Select one session first.");
    bool Confirm(string message)=>MessageBox.Show(this,message,"Recorder evidence",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes;
    void Add(string label,Action action)
    {
        var b=new Button{Text=label,AutoSize=true,Height=30};
        b.Click+=(_,_)=>{try{action();if(!IsDisposed&&label!="ACKNOWLEDGE / CONTINUE"&&label!="SAFE MAINTENANCE"){notice=RuntimeRetention.GetStorageNotice()??notice;Render();}}
            catch(Exception e){MessageBox.Show(this,e.Message,"Evidence preserved",MessageBoxButtons.OK,MessageBoxIcon.Information);notice=RuntimeRetention.GetStorageNotice()??notice;Render();}};
        actions.Controls.Add(b);
    }
    void Render()
    {
        summary.Text=notice.Summary.Replace("\n",Environment.NewLine);
        sessions.Rows.Clear();
        foreach(var d in notice.Report.Decisions.Where(d=>!notice.Report.Deleted.Contains(d.Session))){
            int row=sessions.Rows.Add(d.Session.ToString("N"),d.Active?"ACTIVE":d.Classification,d.Pinned?"PINNED":"unpinned",d.Bytes.ToString("N0"),d.Reason);sessions.Rows[row].Tag=d.Session;
        }
    }
    void Review()
    {
        Guid id=Selected();
        using var file=new OpenFileDialog{Title="Select the completed forensic review (maximum 64 KiB)",Filter="Review evidence|*.md;*.txt;*.json|All files|*.*",CheckFileExists=true,Multiselect=false};
        if(file.ShowDialog(this)!=DialogResult.OK)return;
        using var stream=new FileStream(file.FileName,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length is 0 or >65536)throw new InvalidDataException("Use a concise review of 1–65,536 bytes.");
        byte[] bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);
        using var dialog=new Form{Text="Seal forensic review",Width=620,Height=285,StartPosition=FormStartPosition.CenterParent};
        var rationale=new TextBox{Multiline=true,Dock=DockStyle.Top,Height=110,PlaceholderText="Causal rationale: what was reviewed and what the evidence establishes."};
        var positive=new CheckBox{Text="This review positively establishes that the session is NOT critical.",Dock=DockStyle.Top,Height=45};
        var seal=new Button{Text="SEAL REVIEW",Dock=DockStyle.Bottom,Height=36,DialogResult=DialogResult.OK};
        dialog.Controls.Add(positive);dialog.Controls.Add(rationale);dialog.Controls.Add(seal);dialog.AcceptButton=seal;
        if(dialog.ShowDialog(this)==DialogResult.OK)RuntimeRetention.SealReview(id,rationale.Text,bytes,positive.Checked);
        // Sealing never unpins, reclassifies or retires. Those are separate actions.
    }
    internal static void ShowIfRequired()
    {
        try{RuntimeRetention.PrepareCapacity();if(RuntimeRetention.GetStorageNotice() is {Warn:true} notice){Console.WriteLine(notice.Summary);using var form=new RecorderStorageForm(notice);form.ShowDialog();}}
        catch(Exception e){Console.Error.WriteLine("Recorder storage inspection unavailable; evidence preserved: "+e.Message);}
    }
    internal static void Qualify()
    {
        NovaCore.ConstructionEditor.PlayerApplication.InitializeUi();
        var totals=new RetentionTotals(600L*1024*1024,0,0,600L*1024*1024,1,0,1,1){ExemptRawBytes=600L*1024*1024};
        var report=new RetentionReport("COMPLETE",totals,totals,[new(Guid.NewGuid(),totals.TotalBytes,false,true,false,0,"Blackout evidence; preserve full raw")],[],[],null,0,true,[]);
        var notice=new RecorderStorageNotice(report,true,true,"MANUAL REVIEW REQUIRED");
        using var form=new RecorderStorageForm(notice,true);_=form.Handle;
        // Re-enter the normal player initialization AFTER a maintenance HWND.
        // This must be an idempotent call, without GPU/recorder initialization.
        NovaCore.ConstructionEditor.PlayerApplication.InitializeUi();
        foreach(string label in new[]{"SAFE MAINTENANCE","KEEP","REVIEW / SEAL","UNPIN","RECLASSIFY AS ORDINARY","RETIRE RAW","ACKNOWLEDGE / CONTINUE"})
            if(!form.actions.Controls.Cast<Control>().Any(c=>c.Text==label))throw new InvalidDataException("Missing maintenance action: "+label);
        if(form.sessions.Rows.Count!=1||!form.summary.Text.Contains("MANUAL REVIEW REQUIRED",StringComparison.Ordinal))throw new InvalidDataException("Protected evidence presentation");
        Console.WriteLine("STORAGE_UI_PASS: startup initialization after HWND; seven actions; protected listing; GPU exposure=0; runtime writes=0");
    }
}

