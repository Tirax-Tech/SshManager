using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Tirax.SshManager.Extensions;

public static class BrushColorExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IBrush ToBrush(this uint value) =>
        new ImmutableSolidColorBrush(Color.FromUInt32(value));
}