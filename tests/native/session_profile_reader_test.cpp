#include "memory/session_profile_reader.h"
#include <cassert>
#include <map>
#include <iostream>
struct Memory {
    std::map<uintptr_t,unsigned char> bytes;
    uintptr_t next=0x800000;
    void Store(uintptr_t a,const void* p,size_t n){for(size_t i=0;i<n;++i)bytes[a+i]=static_cast<const unsigned char*>(p)[i];}
    void Ptr(uintptr_t a,uintptr_t value){Store(a,&value,8);}
    uintptr_t Text(const std::string& value){uintptr_t a=next;next+=0x100;struct S{char data[16]{};uint64_t n,cap;}s{};s.n=value.size();s.cap=15;assert(s.n<16);std::memcpy(s.data,value.data(),value.size());Store(a,&s,sizeof(s));return a;}
    void Object(uintptr_t a,const std::string& type,const std::string& name,uintptr_t parent){auto descriptor=next;next+=0x100;Ptr(a+0x18,descriptor);Ptr(descriptor+8,Text(type));Ptr(a+0x68,Text(name));Ptr(a+0x60,parent);}
    void Children(uintptr_t owner,std::initializer_list<uintptr_t> list){auto container=next;next+=0x100;auto begin=next;next+=0x1000;Ptr(owner+0x70,container);Ptr(container,begin);Ptr(container+8,begin+list.size()*16);size_t i=0;for(auto child:list){Ptr(begin+i*16,child);Ptr(begin+i*16+8,0xDEADBEEF);++i;}}
    bool Read(uintptr_t a,void* p,size_t n){for(size_t i=0;i<n;++i)if(!bytes.count(a+i))return false;for(size_t i=0;i<n;++i)static_cast<unsigned char*>(p)[i]=bytes.at(a+i);return true;}
};
int main(){
    Memory m;auto read=[&](uintptr_t a,void* p,size_t n){return m.Read(a,p,n);};cesi::SessionProfileReader<decltype(read)> reader(read);
    uintptr_t base=0x100000,dm=0x200000,workspace=0x300000,players=0x400000,local=0x500000,ve=0x600000,primary=dm-0x1F0;
    m.Object(dm,"DataModel","Ugc",0);m.Object(workspace,"Workspace","Workspace",dm);m.Object(players,"Players","Players",dm);m.Object(local,"Player","FixturePlayer",players);
    m.Children(dm,{workspace,players});m.Children(players,{local});m.Ptr(dm+0x148,workspace);m.Ptr(players+0x118,local);
    m.Ptr(base+cesi::profile::RVA::ActiveDataModelPrimaryA,primary);m.Ptr(base+cesi::profile::RVA::ActiveDataModelPrimaryB,primary);
    m.Ptr(base+cesi::profile::RVA::VisualEngineGlobal,ve);m.Ptr(ve+0xAF0,primary);
    assert(reader.Name(dm)=="Ugc");assert(reader.Children(dm).size()==2);assert(reader.FindActive(base)==dm);
    m.Ptr(base+cesi::profile::RVA::ActiveDataModelPrimaryA,0x700000);assert(reader.FindActive(base)==dm);
    m.Ptr(base+cesi::profile::RVA::ActiveDataModelPrimaryB,0);assert(reader.FindActive(base)==0);
    m.Ptr(base+cesi::profile::RVA::ActiveDataModelPrimaryA,primary);m.Ptr(dm+0x60,workspace);assert(reader.FindActive(base)==0);
    m.Ptr(dm+0x60,0);m.Ptr(local+0x60,workspace);assert(reader.FindActive(base)==0);
    m.Ptr(local+0x60,players);assert(reader.FindActive(base)==dm);assert(reader.FindActive(0)==0);
    std::cout<<"Active experience profile fixture tests passed\n";
}
