using System.Runtime.CompilerServices;
using System.Text;

namespace HyperbolicWarper.Core.Parsing;

internal static class EncodingRegistration
{
    [ModuleInitializer]
    internal static void RegisterCodePages() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
}
