# Virtual Joystick for Stride

<p align="center">
  <img width="958" height="540" alt="VirtualJoystick" src="https://github.com/user-attachments/assets/1b9fc65c-c935-4dc6-afec-7000c1176d76" />
</p>

A responsive, event-driven virtual joystick component for the 'Stride Game Engine', designed for mobile projects that require smooth, intuitive directional input.
This implementation uses two companion libraries for performance and ergonomics:

- [Sharp](https://github.com/Feralnex/Sharp) — general-purpose utilities
- [Sharp.Collections](https://github.com/Feralnex/Sharp.Collections) — lightweight, allocation-friendly collections

The joystick integrates seamlessly with Stride’s UI system and provides both absolute and relative directional input, angle tracking, and radius information.

## Features

- 'Absolute & Relative Input'
  Provides normalized input vectors in both world-aligned and object-relative modes.

- 'Angle & Radius Output'
  Emits angles in radians and degrees, plus a normalized radius (0–1).

- 'Event-Driven Architecture'
  Subscribe to:
  - StartedDragging
  - StoppedDragging
  - AbsoluteInputChanged
  - RelativeInputChanged
  - RadiusChanged
  - AbsoluteAngleChanged
  - RelativeAngleChanged

- 'Resolution-Independent UI'
  Automatically scales and repositions UI elements when the window size changes.

- 'High-Performance Internals'
  Uses:
  - CommunityToolkit.HighPerformance for Span-based iteration
  - Sharp for functional helpers (Value<T>, Reference<T>, etc.)
  - Sharp.Collections for custom event handler lists

- 'Drop-in Stride UI Integration'
  Works with Stride’s UIComponent, Canvas, and ImageElement.

## Installation

### 1. Add dependencies
Install or include the following libraries in your Stride project:

- 'Sharp'
- 'Sharp.Collections'

You can add them manually (NuGet packages are not available yet).

### 2. Add the Virtual Joystick script
Place the following files anywhere inside your Stride game project:

- VirtualJoystick.cs
- IVirtualJoystick.cs
- Angle.cs
- UIElementExtensions.cs

## UI Setup in Stride
Your UI layout must contain the following elements:

```
'Surface' — root Canvas
 ├── 'Zone' — the interactive area (Canvas)
 ├──── 'Threshold' — outer ring image (ContentDecorator)
 └────── 'Thumbstick' — inner stick image
```

These elements must be named exactly:

```
Zone
Threshold
Thumbstick
```

The script automatically locates them using FindVisualChildOfType.

Attach the VirtualJoystick script to the same entity that contains your UIComponent.

## Using the Joystick

### Subscribing to events

```csharp
public override void Start()
{
    var joystick = Entity.Get<IVirtualJoystick>();

    joystick.AbsoluteInputChanged += OnAbsoluteInput;
    joystick.RelativeInputChanged += OnRelativeInput;
    joystick.StartedDragging += pos => { /``` ... ```/ };
    joystick.StoppedDragging += pos => { /``` ... ```/ };
}
```

### Example: Move a character

```csharp
private void OnRelativeInput(Vector2 input)
{
    var direction = new Vector3(input.X, 0, input.Y);
    CharacterComponent.Move(direction);
}
```

## What the Joystick Provides

### Absolute Input  
Normalized vector based on thumbstick position:
(-1..1, -1..1)

### Relative Input  
Same as above, but rotated by the yaw of 'RelativeObject'.

### Radius  
Distance from center (0–1).

### Angles  
- AbsoluteAngleInRadians
- AbsoluteAngleInDegrees
- RelativeAngleInRadians
- RelativeAngleInDegrees

### Example

```csharp
float angle = joystick.AbsoluteAngleInDegrees;
float radius = joystick.Radius;
Vector2 input = joystick.AbsoluteInput;
```

## Architecture Overview

### Sharp.Collections
- References<T> — lightweight event listener lists
- Reference<T> — optional single delegate
- Value<T> — optional value wrapper

### CommunityToolkit.HighPerformance
- Span-based iteration over Stride pointer events
- DangerousGetReferenceAt for zero-allocation access

### Stride UI
- Absolute positioning via SetCanvasAbsolutePosition
- Custom resolution scaling logic

## Resolution & Scaling

The joystick:

- Tracks previous and current resolution
- Scales UI elements proportionally
- Recomputes absolute positions on window resize
- Maintains consistent feel across devices

## Example Scene Structure

```
UI Entity  
 ├── UIComponent (with Page containing Surface/Zone/Threshold/Thumbstick)  
 └── VirtualJoystick (script)  
```

## Usage Notes

Remember to enter correct page design resolution in VirtualJoystick parameters to keep correct scaling.

<p align="center">
  <img width="578" height="524" alt="Desing_resolution_virtual_joystick" src="https://github.com/user-attachments/assets/7b704411-3f69-4e46-b659-f279e9e6a446" />
  <img width="581" height="313" alt="Design_resolution" src="https://github.com/user-attachments/assets/307533f4-3fc9-4e0f-a314-9529a33ce97b" />
</p>


The VirtualJoystick provides a direction based on the orientation of your chosen relative object, allowing your e.g. character to move exactly where the VirtualJoystick points to within that object’s local space.

<p align="center">
  <img width="967" height="524" alt="Relative_object" src="https://github.com/user-attachments/assets/d1404a3b-c33b-4ba8-83db-07d918cb529c" />
</p>
