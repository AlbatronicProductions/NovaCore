// CPU-only Chromium integration: real IndexedDB, synthetic responses and clock.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const {packet,B}=require('./test_packets.cjs');
const out=process.argv[2];if(!out)throw Error('Supply new output directory');fs.mkdirSync(out,{recursive:false});
const checks=[],check=(name,test)=>{assert.ok(test,name);checks.push(name);};
const server=http.createServer((req,res)=>{
  const name=req.url==='/'?'receiver.html':req.url.slice(1);
  if(!['receiver.html','receiver_core.js','receiver_store.js','receiver_app.js'].includes(name)){res.writeHead(404);return res.end();}
  res.setHeader('Content-Type',name.endsWith('.js')?'text/javascript':'text/html');res.end(fs.readFileSync(path.join(__dirname,name),'utf8').replace('__RECEIVER_BUILD__',B));
});
let browser;
(async()=>{
  await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;
  browser=await chromium.launch({headless:true,channel:'msedge',args:['--disable-gpu','--disable-webgl']});const context=await browser.newContext({acceptDownloads:true});const page=await context.newPage();
  const errors=[];page.on('pageerror',e=>errors.push(String(e)));
  await page.addInitScript(({template})=>{
    window.testClock=1000;Object.defineProperty(performance,'now',{value:()=>++window.testClock});
    window.testSerial=0;window.testOffline=false;window.testPrimaryOff=false;
    // Disable automatic polling only: tests explicitly run both production poll
    // paths each logical second and await the real storage commit.
    window.setTimeout=()=>1;window.clearTimeout=()=>{};window.setInterval=()=>1;
    window.fetch=async url=>{
      const primary=!url.startsWith('http:');if(window.testOffline||(primary&&window.testPrimaryOff))throw Error('Synthetic disconnect');
      const nonce=url.split('/').at(-1),t=++window.testClock;
      const d=structuredClone(template);d.nonce=nonce;d.serverMonotonicNs=String(t*1e6);d.remainingSeconds=null;
      d.sample.serial=++window.testSerial;d.localCommittedSerial=d.sample.serial;d.sample.sampleMonotonicNs=d.serverMonotonicNs;d.committedMonotonicNs=d.serverMonotonicNs;
      d.sample.observation.qpc=String(t*10000);d.sample.observation.observerHeartbeat=String(t*10000);
      const result=primary?d:{...d.beacon,nonce,serverMonotonicNs:d.serverMonotonicNs,serverUtcNs:d.serverUtcNs};
      return {ok:true,text:async()=>JSON.stringify(result)};
    };
  },{template:packet('a'.repeat(64),1000)});
  await page.goto(origin);await page.waitForFunction('writer && storageOK');
  await page.evaluate(async()=>{await poll('beacon');await poll('primary');await writer.idle();render();});
  check('production page arms with real IndexedDB transaction/readback',await page.evaluate('state().level')==='FIXTURE ARMED');
  // Fill the old backend to its real browser quota. New storage must not depend on it.
  const legacy=await page.evaluate(()=>{let i=0;try{for(;i<30;i++)localStorage.setItem('nc-blackout:protected-'+i,'x'.repeat(512*1024));}catch(e){return {keys:i,error:e.name};}throw Error('No quota observed');});
  check('legacy localStorage quota is reproduced',legacy.error==='QuotaExceededError');
  await page.evaluate(async()=>{document.getElementById('mark').onclick();await poll('primary');await poll('beacon');await writer.idle();});
  const proof=await page.evaluate('JSON.stringify(capture.marks[0].proofs)');
  const started=Date.now(),checkpoints=[];
  for(let hour=1;hour<=24;hour++){
    const point=await page.evaluate(async()=>{
      for(let s=0;s<3600;s++){
        window.testClock+=1000;await poll('primary');await poll('beacon');await writer.idle();
        if(state().level!=='FIXTURE ARMED')throw Error('Lost ARMED: '+JSON.stringify(state()));
      }
      const saved=await store.load(key);C.validate(saved.value);
      if(window.WitnessStore.normalized(saved.value)!==window.WitnessStore.normalized(capture))throw Error('Ring readback differs');
      return {seconds:Math.floor(window.testClock/1000),rows:capture.rows.length,characters:JSON.stringify(capture).length,dropped:capture.dropped,captures:(await store.list()).length,revision:writer.revision};
    });
    checkpoints.push({hour,...point});console.log('Hour '+hour+': '+JSON.stringify(point));
  }
  check('24-hour accelerated play stays armed despite full old localStorage',checkpoints.length===24&&checkpoints.every(p=>p.captures===1&&p.rows<=2048&&p.characters<=786432));
  check('marker and both fresh nonce proofs survive a full day of rollover',await page.evaluate('JSON.stringify(capture.marks[0].proofs)')===proof);
  // Real store stalls/abort must not be mistaken for a successful request.
  await page.evaluate(async()=>{const real=store.save.bind(store);window.restoreSave=()=>store.save=real;store.save=()=>new Promise(r=>window.releaseSave=()=>r(1));event('blocked-storage');window.testClock+=4000;});
  check('hung commit removes ARMED by acknowledgment age',await page.evaluate('state().level')==='UNQUALIFIED');
  await page.evaluate(async()=>{window.restoreSave();window.releaseSave();await writer.idle();writer.revision=(await store.load(key)).record.revision;event('storage-recovered');await writer.idle();await poll('primary');await poll('beacon');await writer.idle();});
  check('new commits and new responses recover after writer resumes',await page.evaluate('state().level')==='FIXTURE ARMED');
  const abort=await page.evaluate(async()=>{try{await store.transact('readwrite',async t=>{await new Promise((resolve,reject)=>{const r=t.objectStore('captures').get(key);r.onsuccess=resolve;r.onerror=reject;});t.abort();});return false;}catch(e){return true;}});
  check('request success followed by transaction abort is rejected',abort);
  await page.evaluate(async()=>{window.testOffline=true;await poll('primary');await poll('beacon');await writer.idle();});
  check('disconnect becomes stale and does not fabricate host proof',await page.evaluate('state().level')==='RESPONSES STALE');
  await page.evaluate(async()=>{window.testOffline=false;await poll('primary');await poll('beacon');await poll('primary');await writer.idle();window.testPrimaryOff=true;document.getElementById('mark').onclick();window.testClock+=1000;await poll('primary');await poll('beacon');await writer.idle();});
  check('primary loss preserves independent CPU post-marker proof',await page.evaluate('state().level')==='HOST CPU RESPONDING'&&await page.evaluate('!!capture.marks.at(-1).proofs.beacon && !capture.marks.at(-1).proofs.primary'));
  await page.evaluate(async()=>{window.testOffline=true;await poll('beacon');await writer.idle();});
  const downloadEvent=page.waitForEvent('download');await page.evaluate('document.getElementById("export").onclick()');const download=await downloadEvent;
  const file=path.join(out,'day-offline-export.json');await download.saveAs(file);const originalKey=await page.evaluate('key');
  check('offline export seals after commit, with downloaded file present',fs.statSync(file).size>0&&await page.evaluate(async()=>(await store.load(key)).record.sealed));
  await page.locator('#verify-file').setInputFiles(file);await page.waitForFunction('document.getElementById("retire-status").textContent.startsWith("Verified")');
  check('actual downloaded file selection retires exactly the sealed copy',await page.evaluate(async()=>(await store.list()).length)===0);
  // Eight protected captures: transactional admission race admits only one last slot.
  const slots=await page.evaluate(async()=>{
    window.testOffline=false;window.testPrimaryOff=false;const source=C.copy(capture);
    const make=()=>{const v=C.create(source.witness,randomId(),build,Date.now());v.beaconIdentity=source.beaconIdentity;v.sessionBinding=null;return v;};
    window.slotValues=[];for(let i=0;i<7;i++){const v=make();const k=await store.admit(v);slotValues.push({k,v});}
    const a=make(),b=make(),other=new window.WitnessStore.Store();
    const race=await Promise.allSettled([store.admit(a),other.admit(b)]);
    const good=race.find(r=>r.status==='fulfilled');const v=good.value.endsWith(a.pageIncarnation)?a:b;
    window.lastSlot={k:good.value,v};return {admitted:race.filter(r=>r.status==='fulfilled').length,total:(await store.list()).length};
  });
  check('concurrent admission cannot exceed eight protected slots',slots.admitted===1&&slots.total===8);
  const protections=await page.evaluate(async()=>{
    const {k,v}=lastSlot,w=new window.WitnessStore.Writer(store,k,v.pageIncarnation,()=>{});C.append(v,{kind:'still-writing',phoneMono:1,phoneWall:1});w.submit(v);await w.idle();
    let blocked=false;try{await store.retire(JSON.stringify(v));}catch(e){blocked=/active/.test(String(e));}
    const stale=JSON.stringify(v);C.append(v,{kind:'after-download',phoneMono:2,phoneWall:2});w.submit(v);await w.seal(v);
    let mismatch=false;try{await store.retire(stale);}catch(e){mismatch=/match/.test(String(e));}
    const exact=JSON.stringify((await store.load(k)).value);await store.retire(exact);
    let frozen=false;try{w.submit(v);}catch(e){frozen=true;}
    return {blocked,mismatch,frozen,remaining:(await store.list()).length,legacy:localStorage.length};
  });
  check('full store preserves current writer, rejects active/stale file retirement, and fences late saves',protections.blocked&&protections.mismatch&&protections.frozen&&protections.remaining===7&&protections.legacy===legacy.keys);
  const before=await page.evaluate(async()=>JSON.stringify(await store.list()));await page.reload();await page.waitForFunction('writer && storageOK');
  check('reopen creates new page slot while prior protected data remains',await page.evaluate(async()=>(await store.list()).length)===8&&await page.evaluate('key')!==originalKey);
  const beforeFull=await page.evaluate(async()=>JSON.stringify(await store.list()));await page.reload();await page.waitForFunction('admissionError');
  check('ninth reopen fails visibly without deleting any saved evidence',await page.evaluate('state().level')==='UNQUALIFIED'&&await page.evaluate(async()=>JSON.stringify(await store.list()))===beforeFull);
  const frozen=await page.evaluate(async()=>{const selected=slotValues;return !!selected;}).catch(()=>false); // Old JS state must be gone.
  check('refresh cannot inherit a previous page JS owner',!frozen);
  check('browser reports no uncaught page errors',errors.length===0);
  const result={passed:true,scope:'24h accelerated CPU-only production-page/Chromium IndexedDB; not 24h elapsed, Samsung physical storage, GPU or native qualification',checks,legacy,checkpoints,elapsedSeconds:(Date.now()-started)/1000,browser:browser.version(),errors};
  fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close();server.close();});
