/* Bounded IndexedDB ring. Legacy localStorage is read-only, never migrated/deleted. */
(function(root){
  'use strict';
  const C=root.WitnessCore,MAX_CAPTURES=8,DB='nc-blackout-standing-v1';
  const request=r=>new Promise((resolve,reject)=>{r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);});
  const complete=t=>new Promise((resolve,reject)=>{t.oncomplete=resolve;t.onabort=()=>reject(t.error||Error('Storage transaction aborted'));t.onerror=()=>{};});
  const range=key=>IDBKeyRange.bound([key,0],[key,Number.MAX_SAFE_INTEGER]);
  const ordered=v=>Array.isArray(v)?v.map(ordered):v&&typeof v==='object'?Object.fromEntries(Object.keys(v).sort().map(k=>[k,ordered(v[k])])):v;
  const normalized=v=>{const c=C.copy(v);delete c.exportWall;return JSON.stringify(ordered(c));};
  class Store {
    constructor(name=DB){this.name=name;this.db=null;}
    async open(){
      if(this.db)return this;
      const r=indexedDB.open(this.name,1);
      r.onupgradeneeded=()=>{r.result.createObjectStore('captures',{keyPath:'key'});r.result.createObjectStore('rows',{keyPath:['key','sequence']});r.result.createObjectStore('pins',{keyPath:'key'});};
      this.db=await request(r);this.db.onversionchange=()=>{this.db.close();this.db=null;};return this;
    }
    async transact(mode,fn){
      await this.open();const t=this.db.transaction(['captures','rows','pins'],mode),done=complete(t);
      try{const result=await fn(t);await done;return result;}catch(e){try{t.abort();}catch(_){}await done.catch(()=>{});throw e;}
    }
    async admit(value){
      C.validate(value);const key=value.witness+':'+value.pageIncarnation;
      await this.transact('readwrite',async t=>{
        const s=t.objectStore('captures');if(await request(s.count())>=MAX_CAPTURES)throw Error('All 8 saved slots protected. Export, verify a downloaded file, and retire a stopped capture.');
        const {rows,marks,preserved,...base}=C.copy(value);
        await request(s.add({key,owner:value.pageIncarnation,revision:0,sealed:false,base,first:0,last:0}));
        t.objectStore('pins').add({key,marks,preserved});
      });return key;
    }
    snapshot(value){
      // Rows are immutable after append. Copy metadata/pins now so later async work
      // cannot pick up another page, revision, marker, or session.
      const {rows,marks,preserved,...base}=value;
      return {base:C.copy(base),rows:rows.slice(),pins:C.copy({marks,preserved})};
    }
    async save(key,owner,revision,snap,sealed=false){
      await this.transact('readwrite',async t=>{
        const s=t.objectStore('captures'),old=await request(s.get(key));
        if(!old||old.owner!==owner||old.sealed||old.revision!==revision)throw Error('Storage owner/revision is frozen or missing');
        const first=snap.rows[0]?.sequence||snap.base.nextSequence,last=snap.base.nextSequence-1;
        if(first>old.first)t.objectStore('rows').delete(IDBKeyRange.bound([key,0],[key,first],false,true));
        for(const row of snap.rows)if(row.sequence>old.last)t.objectStore('rows').put({key,sequence:row.sequence,row});
        if(!old.base||old.base.pinsRevision!==snap.base.pinsRevision)t.objectStore('pins').put({key,...snap.pins});
        s.put({key,owner,revision:revision+1,sealed,base:snap.base,first,last});
      });
      // Transaction completion, then independent metadata readback. A successful
      // put request alone never makes the page ARMED.
      await this.transact('readonly',async t=>{const r=await request(t.objectStore('captures').get(key));if(!r||r.revision!==revision+1||r.owner!==owner)throw Error('Storage readback mismatch');});
      return revision+1;
    }
    async readIn(t,key){
      const record=await request(t.objectStore('captures').get(key));if(!record?.base)throw Error('Capture incomplete; original retained');
      const rows=await request(t.objectStore('rows').getAll(range(key))),pins=await request(t.objectStore('pins').get(key));
      return {record,value:{...record.base,rows:rows.map(r=>r.row),marks:pins.marks,preserved:pins.preserved}};
    }
    async load(key){return this.transact('readonly',t=>this.readIn(t,key));}
    async list(){return this.transact('readonly',t=>request(t.objectStore('captures').getAll()));}
    async sealAbandoned(key){
      // Explicit operator assertion that the original page is closed; never an
      // age/lease inference. Preserve every byte; fence any late writer.
      return this.transact('readwrite',async t=>{const r=await request(t.objectStore('captures').get(key));if(!r?.base)throw Error('Incomplete capture protected');r.sealed=true;r.revision++;t.objectStore('captures').put(r);});
    }
    async retire(fileText){
      const candidate=JSON.parse(fileText);C.validate(candidate);const key=candidate.witness+':'+candidate.pageIncarnation;
      return this.transact('readwrite',async t=>{
        const {record,value}=await this.readIn(t,key);
        if(!record.sealed)throw Error('Capture is active/unsealed; protected');
        if(normalized(value)!==normalized(candidate))throw Error('Downloaded file does not match the complete saved capture');
        t.objectStore('captures').delete(key);t.objectStore('pins').delete(key);t.objectStore('rows').delete(range(key));return key;
      });
    }
  }
  class Writer {
    constructor(store,key,owner,onStatus){this.store=store;this.key=key;this.owner=owner;this.onStatus=onStatus;this.revision=0;this.pending=null;this.flight=null;this.closing=false;this.error=null;}
    submit(value,submittedMono){if(this.closing)throw Error('Capture sealed');this.pending={snap:this.store.snapshot(value),submittedMono};this.pump();}
    pump(){
      if(this.flight||!this.pending)return;const {snap,submittedMono}=this.pending;this.pending=null;
      this.flight=this.store.save(this.key,this.owner,this.revision,snap).then(r=>{this.revision=r;this.error=null;this.onStatus(null,{submittedMono,pinsRevision:snap.base?.pinsRevision??snap.pinsRevision});},e=>{this.error=e;this.onStatus(e);}).finally(()=>{this.flight=null;this.pump();});
    }
    async idle(){while(this.flight)await this.flight;}
    async seal(value,submittedMono){this.closing=true;await this.idle();const snap=this.store.snapshot(value);try{this.revision=await this.store.save(this.key,this.owner,this.revision,snap,true);this.error=null;this.onStatus(null,{submittedMono,pinsRevision:snap.base?.pinsRevision??snap.pinsRevision});}catch(e){this.error=e;this.onStatus(e);throw e;}}
  }
  root.WitnessStore={Store,Writer,MAX_CAPTURES,DB,normalized};
})(globalThis);
