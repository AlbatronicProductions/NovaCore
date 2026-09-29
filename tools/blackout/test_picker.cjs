// Mobile-picker path regression: real Chromium File inputs and IndexedDB, CPU only.
const fs=require('node:fs'),http=require('node:http'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_MODULE||'playwright');
const {packet,B}=require('./test_packets.cjs');
const out=process.argv[2];fs.mkdirSync(out,{recursive:false});const checks=[];
const server=http.createServer((req,res)=>{const name=req.url==='/'?'receiver.html':req.url.slice(1);if(!['receiver.html','receiver_core.js','receiver_store.js','receiver_app.js'].includes(name)){res.writeHead(404);return res.end();}res.setHeader('Content-Type',name.endsWith('.js')?'text/javascript':'text/html');res.end(fs.readFileSync(path.join(__dirname,name),'utf8').replace('__RECEIVER_BUILD__',B));});
let browser;
(async()=>{
  await new Promise(r=>server.listen(0,'127.0.0.1',r));browser=await chromium.launch({channel:'msedge',headless:true,args:['--disable-gpu','--disable-webgl']});const page=await browser.newPage();
  await page.addInitScript(template=>{window.template=template;window.fetch=async url=>({ok:true,text:async()=>JSON.stringify({...template,nonce:url.split('/').at(-1),mode:'recovery',sample:null})});},packet('a'.repeat(64),1000));
  await page.goto('http://127.0.0.1:'+server.address().port);await page.waitForFunction('latest?.mode==="recovery"');
  assert.equal(await page.locator('#verify-file').getAttribute('accept'),null);checks.push('unrestricted picker admits arbitrary Android MIME/extension');
  const seed=async sealed=>page.evaluate(async sealed=>{
    const v=C.create(template.witness,randomId(),build,Date.now());v.beaconIdentity=template.beacon;v.sessionBinding=null;
    const k=await store.admit(v);C.append(v,{kind:'control',phoneWall:1,phoneMono:1});await store.save(k,v.pageIncarnation,0,store.snapshot(v),sealed);
    return {k,text:JSON.stringify(v)};
  },sealed);
  const protectedCapture=await seed(false);
  for(const [index,mimeType] of ['','text/plain','application/octet-stream','application/json','image/png'].entries()){
    const value=await seed(true),name=index===4?'valid-content.unrecognized':'downloaded-witness-'+index+'.json';
    const local=path.join(out,name);fs.writeFileSync(local,value.text);const before=fs.readFileSync(local);
    if(index===0)await page.evaluate(()=>{const original=File.prototype.text;File.prototype.text=function(){const file=this;return new Promise(resolve=>window.releaseRead=()=>resolve(original.call(file)));};window.restoreRead=()=>File.prototype.text=original;});
    await page.locator('#verify-file').setInputFiles({name,mimeType,buffer:before});
    if(index===0){
      assert.equal(await page.locator('#selected-files').textContent(),'Selected: '+name);
      assert.ok((await page.locator('#retire-status').textContent()).startsWith('Verifying'));
      assert.equal(await page.evaluate(async()=> (await store.list()).length),2);
      await page.evaluate(()=>{window.restoreRead();window.releaseRead();});checks.push('filename appears before asynchronous file validation; no premature retirement');
    }
    await page.waitForFunction('document.getElementById("retire-status").textContent.startsWith("Verified")');
    assert.equal(await page.locator('#verify-file').evaluate(e=>e.files[0].name),name);
    assert.equal(await page.evaluate(async()=> (await store.list()).length),1);
    assert.deepEqual(fs.readFileSync(local),before);checks.push('exact match retires only stopped copy; selection and source file retained: MIME '+(mimeType||'(empty)'));
    await page.evaluate(async()=>{await refreshSaved();document.dispatchEvent(new Event('visibilitychange'));render();});
    assert.equal(await page.locator('#verify-file').evaluate(e=>e.files[0].name),name);
  }
  const stopped=await seed(true),mismatch=JSON.parse(stopped.text);mismatch.createdWall++;
  for(const [name,text] of [['malformed.json','{"broken":'],['active.json',protectedCapture.text],['mismatched.json',JSON.stringify(mismatch)]]){
    await page.locator('#verify-file').setInputFiles({name,mimeType:'application/json',buffer:Buffer.from(text)});
    await page.waitForFunction('document.getElementById("retire-status").textContent.includes("no matching saved copy retired")');
    assert.equal(await page.evaluate(async()=> (await store.list()).length),2);checks.push(name+' leaves all saved evidence intact');
  }
  const result={passed:true,scope:'Synthetic Chromium picker/File/IndexedDB tests; physical Samsung Internet smoke still required',checks,browser:browser.version()};fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({output:out,...result},null,2));
})().catch(e=>{console.error(e);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close();server.close();});

