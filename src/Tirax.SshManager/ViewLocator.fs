namespace Tirax.SshManager

open System
open Avalonia.Controls
open Avalonia.Controls.Templates
open Tirax.SshManager.ViewModels

type ViewLocator() =
    interface IDataTemplate with
        
        member this.Build(data) =
            let name = data.GetType().FullName.Replace("ViewModel", "View")
            let typ = Type.GetType(name)
            if isNull typ then
                upcast TextBlock(Text = $"Not Found: %s{name}")
            else
                downcast Activator.CreateInstance(typ)

        member this.Match(data) = data :? ViewModelBase