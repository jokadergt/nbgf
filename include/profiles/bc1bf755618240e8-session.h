#pragma once
#include <cstdint>
// Perfil observado: version-bc1bf755618240e8
// SHA-256: 57E5C4203D420815D7704FA53FA1A63E09D1190E4A55439356B41B1101AF2120
// Somente descoberta estrutural; nao certifica execucao de scripts.
// Nenhum endereco absoluto de heap e exportado como constante.
namespace roblox_bc1bf755618240e8 {
namespace Instance { inline constexpr std::uintptr_t ClassDescriptor = 0x18; } // CONFIRMADO NESTA SESSAO: Instance -> descriptor; classes legiveis em multiplos servicos, jogadores e modulos.
namespace ClassDescriptor { inline constexpr std::uintptr_t Name = 0x8; } // CONFIRMADO NESTA SESSAO: descriptor -> string de classe.
namespace Instance { inline constexpr std::uintptr_t Name = 0x68; } // CONFIRMADO NESTA SESSAO: nome via ponteiro para MSVC string; multiplos objetos.
namespace Instance { inline constexpr std::uintptr_t Parent = 0x60; } // CONFIRMADO NESTA SESSAO: 154 filhos diretos do Ugc com parent reciproco.
namespace Instance { inline constexpr std::uintptr_t Children = 0x70; } // CONFIRMADO NESTA SESSAO: vetores de DataModel, Workspace, Players, personagem e servicos.
namespace ChildrenVector { inline constexpr std::uintptr_t Begin = 0x0; } // CONFIRMADO NESTA SESSAO: ponteiro inicial.
namespace ChildrenVector { inline constexpr std::uintptr_t End = 0x8; } // CONFIRMADO NESTA SESSAO: limite exclusivo.
namespace ChildrenVector { inline constexpr std::uintptr_t Stride = 0x10; } // CONFIRMADO NESTA SESSAO: entradas de 16 bytes; somente primeiro ponteiro interpretado.
namespace MsvcString { inline constexpr std::uintptr_t Length = 0x10; } // CONFIRMADO NESTA SESSAO: layout compativel com nomes observados.
namespace MsvcString { inline constexpr std::uintptr_t Capacity = 0x18; } // CONFIRMADO NESTA SESSAO: capacidade decide inline <=15 ou heap.
namespace DataModel { inline constexpr std::uintptr_t Workspace = 0x148; } // CONFIRMADO NESTA SESSAO: identidade coincide com filho Workspace.
namespace Players { inline constexpr std::uintptr_t LocalPlayer = 0x118; } // CONFIRMADO NESTA SESSAO: aponta a Player filho de Players; personagem carregado.
namespace TaskScheduler { inline constexpr std::uintptr_t JobStart = 0xC8; } // CONFIRMADO NESTA SESSAO: limites validos; 70 jobs.
namespace TaskScheduler { inline constexpr std::uintptr_t JobEnd = 0xD0; } // CONFIRMADO NESTA SESSAO: limite exclusivo.
namespace Jobs { inline constexpr std::uintptr_t Stride = 0x10; } // CONFIRMADO NESTA SESSAO: 70 nomes legiveis e RenderJob valido.
namespace Jobs { inline constexpr std::uintptr_t Name = 0x18; } // CONFIRMADO NESTA SESSAO: MSVC string embutida.
namespace RenderJob { inline constexpr std::uintptr_t RenderView = 0x1E0; } // CONFIRMADO NESTA SESSAO: um RenderJob nesta sessao.
namespace RenderView { inline constexpr std::uintptr_t VisualEngine = 0x20; } // CONFIRMADO NESTA SESSAO: identidade coincide com global independente.
namespace VisualEngine { inline constexpr std::uintptr_t PrimaryDataModel = 0xAF0; } // CONFIRMADO NESTA SESSAO: ponteiro primary; somar ajuste, nao desreferenciar ajuste.
namespace RenderView { inline constexpr std::uintptr_t PrimaryDataModel = 0x258; } // CONFIRMADO NESTA SESSAO: converge com tres cadeias globais.
namespace PointerAdjustment { inline constexpr std::uintptr_t PrimaryToInstance = 0x1F0; } // CONFIRMADO NESTA SESSAO: adicao ao primary; classe DataModel e 154 pais reciprocos.
namespace Player { inline constexpr std::uintptr_t UserId = 0xB8; } // CANDIDATO: quatro inteiros uint64 plausiveis; identidade numerica externa nao corroborada.
namespace Player { inline constexpr std::uintptr_t Character = 0x2A0; } // CANDIDATO: um Player; coincide com Model homonimo no Workspace, com Humanoid e HumanoidRootPart.
namespace ModuleScript { inline constexpr std::uintptr_t Storage = 0x120; } // CANDIDATO: oito ModuleScripts; tamanhos/capacidades validos, sem decodificacao integral.
namespace SharedString { inline constexpr std::uintptr_t Data = 0x10; } // CANDIDATO: inline ou ponteiro conforme capacidade; oito amostras.
namespace SharedString { inline constexpr std::uintptr_t Size = 0x20; } // CANDIDATO: oito tamanhos coerentes.
namespace SharedString { inline constexpr std::uintptr_t Capacity = 0x28; } // CANDIDATO: oito capacidades >= tamanho.
namespace Instance { inline constexpr std::uintptr_t PropertyStore = 0x38; } // CANDIDATO: container compativel em seis classes; ligacao do getter a Instance requer confirmar ajuste de this.
namespace PropertyStore { inline constexpr std::uintptr_t Begin = 0x0; } // CANDIDATO: vetor de entradas.
namespace PropertyStore { inline constexpr std::uintptr_t End = 0x8; } // CANDIDATO: limite exclusivo.
namespace PropertyStore { inline constexpr std::uintptr_t Overflow = 0x18; } // CANDIDATO: nulo nas amostras; ramo segmentado observado no codigo.
namespace PropertyEntry { inline constexpr std::uintptr_t Stride = 0x10; } // CANDIDATO: codigo de lookup incrementa 0x10.
namespace PropertyEntry { inline constexpr std::uintptr_t Key = 0x8; } // CANDIDATO: comparacao uint16 no lookup; nao uint32/uint8.
namespace PropertyEntry { inline constexpr std::uintptr_t Value = 0x0; } // CANDIDATO: getter retorna primeiro qword da entrada encontrada.
namespace RVA { inline constexpr std::uintptr_t VisualEngineGlobal = 0x8942CB8; } // confirmado nesta sessao; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t TaskSchedulerGlobal = 0x8E4D5F8; } // confirmado nesta sessao; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t ActiveDataModelPrimaryA = 0x8EB67A0; } // confirmado nesta sessao; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t ActiveDataModelPrimaryB = 0x8EB7D80; } // confirmado nesta sessao; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t CapabilitiesKey = 0x8E32D34; } // candidato; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t CapabilitiesGetter = 0x28CF1C0; } // candidato; somar a base atual.
namespace RVA { inline constexpr std::uintptr_t PropertyLookup = 0xDA2420; } // confirmado nesta sessao: algoritmo; vinculo semantico candidato; somar a base atual.
// PENDENTE: offset fixo de DataModel::ScriptContext, capabilities efetivas,
// getter binding, bytecode decoding e validacao apos reinicio.
// Localizar ScriptContext por classe na arvore; nao exportar 0x440 como valido.
// PropertyEntry::Key representa uint16_t, apesar do alinhamento de 8 bytes.
// Aplicar PointerAdjustment::PrimaryToInstance por soma, sem ler nessa etapa.
}

