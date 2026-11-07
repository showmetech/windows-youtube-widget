const fs=require('fs'),vm=require('vm'),assert=require('assert');
const root='extension/';
const m=JSON.parse(fs.readFileSync(root+'manifest.json','utf8'));
assert.equal(m.manifest_version,3);assert.deepEqual(m.permissions,['nativeMessaging','storage']);assert(!m.host_permissions);assert.equal(m.content_scripts[0].world,'ISOLATED');
for(const file of [...Object.values(m.icons),m.background.service_worker,m.options_ui.page,...m.content_scripts[0].js])assert(fs.existsSync(root+file),file);
let listener,forwarded=0;
const context={URL,chrome:{runtime:{id:'test-extension',onMessage:{addListener:f=>listener=f},sendNativeMessage:async()=>{forwarded++;return{ok:true,phase:'playing'};}},storage:{local:{get:(defaults,cb)=>cb?cb(defaults):Promise.resolve(defaults),set:async()=>{throw Error('storage unavailable');}}}}};
vm.runInNewContext(fs.readFileSync(root+'background.js','utf8'),context);
const sender={id:'test-extension',frameId:0,tab:{id:1},url:'https://www.youtube.com/watch?v=M7lc1UVf-VE'};
const message={type:'handoff',phase:'prepare',id:'M7lc1UVf-VE',title:'Test',seconds:10,volume:30,muted:false,playing:true,rate:1,live:false};
const settle=()=>new Promise(r=>setImmediate(r));
(async()=>{
 for(const bad of [{...sender,id:'other'},{...sender,frameId:1},{...sender,url:'https://www.youtube.com.evil.test/'},{...sender,tab:undefined}])listener(message,bad,()=>assert.fail('Untrusted sender replied'));
 assert.equal(forwarded,0);
 for(const bad of [{...message,seconds:NaN},{...message,volume:101},{...message,id:'bad'},{...message,phase:'commit',transferId:'bad'},{...message,playing:'true'}]){let reply;listener(bad,sender,r=>reply=r);assert.equal(reply.ok,false);}
 assert.equal(forwarded,0);
 let reply;listener(message,sender,r=>reply=r);await settle();await settle();assert.equal(reply.ok,true);assert.equal(forwarded,1);
 let disabled;listener({type:'recommendation',id:'M7lc1UVf-VE'},sender,r=>disabled=r);await settle();assert.equal(disabled.reason,'disabled');assert.equal(forwarded,1);
 console.log('PASS: MV3/assets; minimal permissions; 4 sender boundaries; 5 invalid payloads; storage failure preserves success; recommendations opt-in.');
})().catch(e=>{console.error(e);process.exitCode=1;});
