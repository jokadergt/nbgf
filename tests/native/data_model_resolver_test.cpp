#include "memory/data_model_resolver.h"
#include <map>
#include <cassert>
#include <iostream>
struct Memory {
    std::map<uintptr_t, unsigned char> bytes;
    void Store(uintptr_t address, const void* source, size_t size) {
        for(size_t i=0;i<size;++i) bytes[address+i]=static_cast<const unsigned char*>(source)[i];
    }
    void Pointer(uintptr_t address, uintptr_t value) { Store(address,&value,sizeof(value)); }
    bool Read(uintptr_t address, void* output, size_t size) {
        for(size_t i=0;i<size;++i) if(!bytes.count(address+i)) return false;
        for(size_t i=0;i<size;++i) static_cast<unsigned char*>(output)[i]=bytes.at(address+i);
        return true;
    }
};
int main() {
    Memory m;
    const uintptr_t base=0x100000, fake=0x200000, real=0x300000, desc=0x400000, name=0x500000;
    cesi::DataModelLayout l{0x100,0x1f8,0x108,0xaf0,0x110,0xc8,0xd0,0x1f0,0x18,0x8};
    auto read=[&](uintptr_t a,void* b,size_t n){return m.Read(a,b,n);};
    cesi::DataModelResolver<decltype(read)> resolver(read,l);
    m.Pointer(base+l.fakeGlobal,fake);m.Pointer(fake+l.fakeToReal,real);
    m.Pointer(real+l.classDescriptor,desc);m.Pointer(desc+l.className,name);
    struct S{char text[16];uint64_t size,capacity;} str{};
    std::memcpy(str.text,"DataModel",9);str.size=9;str.capacity=15;m.Store(name,&str,sizeof(str));
    assert(resolver.Find(base)==real);
    assert(resolver.Find(base,real)==real);
    assert(resolver.Find(0)==0);
    // The obsolete 0x1c0 chain cannot produce a false positive.
    m.Pointer(fake+l.fakeToReal,0);m.Pointer(fake+0x1c0,real);
    assert(resolver.Find(base)==0);
    auto visual=0x600000ULL;m.Pointer(base+l.visualGlobal,visual);
    m.Pointer(visual+l.visualToFake,fake);m.Pointer(fake+l.fakeToReal,real);
    assert(resolver.Find(base)==real);
    m.Pointer(base+l.fakeGlobal,0);m.Pointer(visual+l.visualToFake,0);
    auto scheduler=0x700000ULL,jobs=0x800000ULL,job=0x900000ULL;
    m.Pointer(base+l.schedulerGlobal,scheduler);m.Pointer(scheduler+l.jobsBegin,jobs);
    m.Pointer(scheduler+l.jobsEnd,jobs+8);m.Pointer(jobs,job);m.Pointer(job+l.renderJobToReal,real);
    assert(resolver.Find(base)==real);
    m.Pointer(scheduler+l.jobsEnd,jobs+8*5000);assert(resolver.Find(base)==0);
    m.Pointer(scheduler+l.jobsEnd,jobs+8);str.text[0]='X';m.Store(name,&str,sizeof(str));
    assert(!resolver.IsDataModel(real));assert(resolver.Find(base,real)==0);
    str.text[0]='D';str.capacity=1;m.Store(name,&str,sizeof(str));assert(!resolver.IsDataModel(real));
    std::cout<<"DataModel fixture tests passed\n";
}
