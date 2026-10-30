// The assembly may be called Jumbus while the code keeps the WebShop
// namespace. Resource lookup goes by the assembly name unless told otherwise,
// and would then search for Jumbus.Resources.SharedResource and show raw keys
// instead of text. This line points it back at the namespace the .resx files
// are compiled under.
[assembly: Microsoft.Extensions.Localization.RootNamespace("WebShop")]

namespace WebShop;

/// <summary>
/// Marker type for strings shared across views and controllers. Its only job
/// is to give IStringLocalizer&lt;SharedResource&gt; a key into
/// Resources/SharedResource.{culture}.resx.
///
/// It has to live in the project's root namespace. With ResourcesPath set to
/// "Resources", the localiser derives the resource name from the type's
/// namespace: a type in WebShop.Infrastructure would be looked up under
/// Resources/Infrastructure/SharedResource.resx instead.
/// </summary>
public sealed class SharedResource
{
}
