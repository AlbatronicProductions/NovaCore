'use strict';
import {extents,sockets,socketMarkerLimit} from './geometry.mjs';
const $=id=>document.getElementById(id),token=document.querySelector('meta[name=editor-token]').content;
let state=null,busy=false;
const selectedDefinition=()=>state.catalog.find(d=>d.index===Number($('definition').value));
const partDefinition=id=>{const p=state.current?.data.instances.find(p=>p.id===id);return p&&state.catalog.find(d=>d.id===p.definition.id&&d.revision===p.definition.revision);};
function options(id,items,value=x=>x.id,label=x=>x.id){const element=$(id),old=element.value;element.replaceChildren();for(const item of items){const o=document.createElement('option');o.value=value(item);o.textContent=label(item);element.append(o);}if([...element.options].some(o=>o.value===old))element.value=old;}
function status(text,error=false){$('status').textContent=text;$('status').classList.toggle('error',error);}
async function request(path,body){const response=await fetch(path,{method:body?'POST':'GET',headers:{'X-Editor-Token':token,...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined});if(!response.ok){const e=await response.json().catch(()=>({error:`HTTP ${response.status}`}));throw new Error(e.error);}return response;}
async function edit(operation,fields={}){if(busy)return;busy=true;let committed=false;document.querySelectorAll('button').forEach(b=>b.disabled=true);try{const r=await request('/api/edit',{operation,revision:state.revision,...fields});state=await r.json();committed=true;render();status(`${operation}: accepted. Draft revision ${state.revision}.`);}catch(e){status(committed?`Design accepted; view could not refresh. Reload this page: ${e.message}`:`Request refused or failed: ${e.message}`,true);}finally{busy=false;document.querySelectorAll('button').forEach(b=>b.disabled=false);}}
function services(){return ['propellant','electricity','data'].filter(id=>$(id).checked).map(s=>s[0].toUpperCase()+s.slice(1)).join(', ')||'None';}
function connection(){return {parent:$('parent').value,parentInterface:$('parentInterface').value,childInterface:$('childInterface').value,connectionId:$('connectionId').value,services:services(),detachable:$('detachable').checked};}
function ports(){const parent=partDefinition($('parent').value),child=$('mode').value==='reconnect'?partDefinition($('selected').value):selectedDefinition();options('parentInterface',parent?.attachments||[],a=>a.id,a=>`${a.id} · ${a.family}`);options('childInterface',child?.attachments||[],a=>a.id,a=>`${a.id} · ${a.family}`);}
function inspect(){const id=$('selected').value,part=state.current?.parts.find(p=>p.id===id),definition=partDefinition(id);$('inspection').textContent=part?JSON.stringify({part,definition},null,2):'No placed part.';$('config').value=JSON.stringify(state.current?.data.configuration.find(c=>c.part===id)||{},null,2);ports();draw();}
function render(){
    $('revision').textContent=`Draft revision ${state.revision}`;
    $('runtime').textContent=state.runtime?`Runtime ${state.runtime.generation} · ${state.runtime.parts} parts · ${state.runtime.subparts} subparts · ${state.runtime.stores} stores · ${state.runtime.actuators} actuators · control ${state.runtime.controlPart??'none'} · reference mass ${state.runtime.referenceMass===null?'unqualified partial fill':state.runtime.referenceMass.toFixed(3)+' kg'} · design ${state.runtime.digest.slice(0,16)} · ${state.runtime.scope}`:'No runtime instance.';
    options('definition',state.catalog,d=>d.index,d=>`${d.name} · r${d.revision}`);options('stock',state.stocks,s=>s.index,s=>s.name);
    const parts=state.current?.data.instances||[];options('parent',parts);options('selected',parts);
    const def=selectedDefinition();$('definitionInfo').textContent=def?`${def.development?'Development content':'Catalog part'} · dry ${def.dryMassKg.toFixed(2)} kg · ${def.attachments.length} interfaces · ${def.subparts.length} owned subparts`:'';
    let n=1;while(parts.some(p=>p.id===`p${n}`))n++;$('instance').value=`p${n}`;
    const connections=state.current?.data.connections||[];n=1;while(connections.some(c=>c.construction.id===`c${n}`))n++;$('connectionId').value=`c${n}`;
    $('summary').textContent=state.current?`${parts.length} parts · dry mass ${state.current.mass.toFixed(3)} kg · design ${state.current.data.id} / r${state.current.data.revision} · ${state.current.digest.slice(0,16)}${state.preview?' · amber preview awaiting acceptance':''}`:(state.preview?'Root preview awaiting acceptance.':'Empty design.');
    $('graphs').textContent=JSON.stringify({connections,serviceLinks:state.current?.data.serviceLinks||[],fuel:state.current?.fuel||[],electrical:state.current?.electrical||[],commandAndPower:state.current?.parts.map(p=>({id:p.id,powerBus:p.powerBus,dataBus:p.dataBus,commandReachable:p.commandReachable}))||[]},null,2);
    $('metadata').value=JSON.stringify({symmetry:state.current?.data.symmetry||[],actions:state.current?.data.actions||[],serviceLinks:state.current?.data.serviceLinks||[]},null,2);inspect();
}
function draw(){
    const svg=$('scene');svg.replaceChildren();const ns='http://www.w3.org/2000/svg';
    function node(type,attributes,text){const e=document.createElementNS(ns,type);for(const [k,v]of Object.entries(attributes))e.setAttribute(k,v);if(text!==undefined)e.textContent=text;svg.append(e);return e;}
    const axes=$('projection').value.toLowerCase().split(''),docs=[state.current,state.preview].filter(Boolean);
    const definitions=new Map(state.catalog.map(d=>[`${d.id}@${d.revision}`,d])),definitionFor=p=>definitions.get(`${p.definition}@${p.definitionRevision}`);
    const bounds=extents(docs,definitionFor,axes);
    if(!bounds){node('text',{x:500,y:230,'text-anchor':'middle',fill:'#8298ad','font-size':18},'Choose a definition and preview a root.');return;}
    const loX=bounds.minX-1,hiX=bounds.maxX+1,loY=bounds.minY-1,hiY=bounds.maxY+1;
    const scale=Math.min(860/(hiX-loX),340/(hiY-loY)),map=p=>[500+(p[axes[0]]-(loX+hiX)/2)*scale,230-(p[axes[1]]-(loY+hiY)/2)*scale];
    node('text',{x:20,y:28,fill:'#8ca5bb','font-size':13},`${axes[0].toUpperCase()} → / ${axes[1].toUpperCase()} ↑ · metres · circles = material origins; diamonds = sockets; crosses = dry COM`);
    let markers=0;for(const d of docs){const ghost=d===state.preview,color=ghost?'#f4b664':'#64cfc3';
        for(const edge of d.data.connections){const a=map(d.parts.find(p=>p.id===edge.parent).pose.position),b=map(d.parts.find(p=>p.id===edge.child).pose.position);node('line',{x1:a[0],y1:a[1],x2:b[0],y2:b[1],stroke:color,'stroke-width':2,'stroke-dasharray':ghost?'6 5':'none',opacity:.6});}
        for(const p of d.parts){const [cx,cy]=map(p.pose.position),[mx,my]=map(p.com);const circle=node('circle',{cx,cy,r:9,fill:ghost?'none':'#182e3b',stroke:p.id===$('selected').value&&!ghost?'#fff':color,'stroke-width':2});circle.style.cursor='pointer';circle.onclick=()=>{if(!ghost){$('selected').value=p.id;inspect();}};
            node('text',{x:cx+13,y:cy-10,fill:color,'font-size':13},p.id+(ghost?' · preview':''));node('path',{d:`M${mx-4},${my}h8 M${mx},${my-4}v8`,stroke:color,fill:'none'});
            for(const socket of sockets(p,definitionFor(p))){if(markers>=socketMarkerLimit)break;markers++;const [sx,sy]=map(socket.position);const dot=node('path',{d:`M${sx},${sy-4}l4,4 -4,4 -4,-4z`,fill:color,opacity:.8});const title=document.createElementNS(ns,'title');title.textContent=`${p.id} / ${socket.id} (${socket.family})`;dot.append(title);}
        }
    }if(bounds.count>socketMarkerLimit+2*docs.reduce((n,d)=>n+d.parts.length,0))node('text',{x:20,y:440,fill:'#f4b664','font-size':13},`Showing ${socketMarkerLimit} socket markers; full interfaces remain in selection and inspection.`);
}
$('root').onclick=()=>edit('previewRoot',{definitionIndex:Number($('definition').value),instance:$('instance').value,designId:$('designId').value});
$('preview').onclick=()=>edit($('mode').value==='reconnect'?'previewReconnect':'previewAttach',{...connection(),definitionIndex:Number($('definition').value),instance:$('mode').value==='reconnect'?$('selected').value:$('instance').value});
$('accept').onclick=()=>edit('accept');$('cancel').onclick=()=>edit('cancel');$('clear').onclick=()=>edit('clear');$('remove').onclick=()=>edit('remove',{instance:$('selected').value});$('control').onclick=()=>edit('control',{instance:$('selected').value});
document.querySelectorAll('[data-axis]').forEach(b=>b.onclick=()=>edit('rotate',{axis:b.dataset.axis}));
$('applyConnection').onclick=()=>edit('connection',connection());$('applyConfig').onclick=()=>edit('config',{payload:$('config').value});$('applyMetadata').onclick=()=>edit('metadata',{payload:$('metadata').value});
$('loadStock').onclick=()=>edit('stock',{stockIndex:Number($('stock').value)});
$('instantiate').onclick=()=>edit('instantiate',{runtimeGeneration:state.runtime?.generation??null});$('retire').onclick=()=>edit('retire',{runtimeGeneration:state.runtime?.generation??null});
$('save').onclick=async()=>{try{const response=await request(`/api/save?revision=${encodeURIComponent(state.revision)}`),url=URL.createObjectURL(await response.blob()),a=document.createElement('a');a.href=url;a.download='vehicle-design.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);status('Canonical design downloaded.');}catch(e){status(e.message,true);}};
$('load').onchange=async()=>{const file=$('load').files[0];if(file){if(file.size>4_000_000)status('Design file exceeds 4 MB.',true);else await edit('load',{payload:await file.text()});}$('load').value='';};
$('definition').onchange=()=>{const d=selectedDefinition();$('definitionInfo').textContent=`${d.name} · dry ${d.dryMassKg.toFixed(2)} kg · ${d.attachments.length} interfaces · ${d.subparts.length} owned subparts`;ports();};
$('parent').onchange=ports;$('mode').onchange=ports;$('selected').onchange=inspect;$('projection').onchange=draw;
(async()=>{try{state=await(await request('/api/state')).json();render();status('Ready. All catalog assets verified at session startup.');}catch(e){status(e.message,true);}})();
