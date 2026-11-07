const fs=require('fs'),vm=require('vm'),assert=require('assert');
let nativeListener,messageListener,disconnectListener,enabled=true,created=[],removed=[],nativeSent=[];
const port={onMessage:{addListener:f=>nativeListener=f},onDisconnect:{addListener:f=>disconnectListener=f},postMessage:m=>nativeSent.push(m),disconnect(){}};
const context={URL,Map,setTimeout:()=>1,clearTimeout(){},chrome:{runtime:{id:'own',onMessage:{addListener:f=>messageListener=f},onStartup:{addListener(){}},onInstalled:{addListener(){}},connectNative:()=>port,sendNativeMessage:async(h,m)=>{nativeSent.push(m);return{ok:true};}},storage:{onChanged:{addListener(){}},local:{get:(defaults,callback)=>callback?callback({enabled}):Promise.resolve({enabled}),set:async()=>{}}},tabs:{create:async options=>{created.push(options);return{id:42};},get:async()=>({active:false}),remove:async id=>removed.push(id)}}};
const settle=()=>new Promise(r=>setImmediate(r));
(async()=>{
 vm.runInNewContext(fs.readFileSync('extension/background.js','utf8'),context);await settle();assert.equal(nativeSent[0].type,'subscribeRecommendations');
 const token='a'.repeat(32);await nativeListener({type:'refreshRecommendations',token});assert.equal(created.length,1);assert.equal(created[0].active,false);assert.equal(created[0].url,'https://www.youtube.com/');let contextReply;messageListener({type:'recommendationContext'},{id:'own',frameId:0,tab:{id:42},url:created[0].url},r=>contextReply=r);assert.equal(contextReply.refreshToken,token);
 await nativeListener({type:'refreshRecommendations',token});assert.equal(created.length,1);
 let reply;messageListener({type:'recommendation',refreshToken:token,id:'M7lc1UVf-VE',title:'New recommendation',videos:[]},{id:'own',frameId:0,tab:{id:42},url:created[0].url},r=>reply=r);await settle();await settle();assert(reply.ok);assert.equal(nativeSent.at(-1).refreshToken,token);assert.deepEqual(removed,[42]);
 enabled=false;await nativeListener({type:'refreshRecommendations',token:'b'.repeat(32)});assert.equal(created.length,1);
 console.log('PASS: native subscription, background homepage, request deduplication, correlated fresh response, own-tab cleanup, disabled recommendations respected.');
})().catch(e=>{console.error(e);process.exitCode=1;});
