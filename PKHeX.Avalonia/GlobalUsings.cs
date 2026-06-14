// Transitional global usings (removed in Phase 6 once the host's own files carry explicit usings).
// While services/ViewModels migrate out of PKHeX.Avalonia.* into the inner layers, these keep the
// host (App, Views, Services, Converters, and the not-yet-moved ViewModels) compiling without
// touching every file's using list mid-migration.
global using PKHeX.Application.Abstractions;
global using PKHeX.Application.Services;
global using PKHeX.Infrastructure;
