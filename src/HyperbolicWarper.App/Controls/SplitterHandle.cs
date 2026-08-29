using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace HyperbolicWarper.App.Controls;

// UIElement.ProtectedCursor is protected, so it can only be set from within a class that
// derives from the element itself -- there's no public ChangeCursor API in this SDK version.
// This thin Grid subclass exists solely to expose that as a public method (Border is sealed
// in this SDK, so Grid stands in as the container -- it supports Background the same way).
public class SplitterHandle : Grid
{
    public void SetCursor(InputSystemCursorShape shape)
    {
        ProtectedCursor = InputSystemCursor.Create(shape);
    }
}
