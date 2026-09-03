# UI & Interface
User will have to interact with multiple plugins, via presets, specialized for each plugin.
Each preset will have individual feature blocks, as well as a list of settings, consistent between all features,
(Note: Feature and Feature block terms might be changed later)

To generalize - list of settings, and individual features, are considered "components" from now and onward.
List of settings - main component, or parent component.
Feature blocks - simply components.

Each component will have a DataContext attached to them, with an ability to save its states to settings.
For this, each DataContext will be created as brand-new when you add them, and will have a GUID asigned to them.
GUID will be used to find a data entry, corresponding to their settings.
Data entry is assigned once, on app start or when a component is first created.

# Architecture
We will use standard MVVM pattern.
Services (Models) will implement ObservableObject for state callbacks.
## Dispatching
ViewModel's job will be to read value change events, and store new states locally in its own DataContext.
In case multiple state changes happen before a Dispatch - ViewModel will update the states using the latest state.
For that reason, all publicly exposed values in all services should be **atomic**.
In specific cases, should also only be modified under a lock.
## Plugin interactions
For simplicity, all plugin interactions will happen within one preset.
For example:

- VTubeStudio preset can never interact with Warudo preset.
- All Features you can add in VTubeStudio preset are specialized to work with VTubeStudio specifically.
- Features, such as facial expression interpretation, has unique API for VTS, and might not have a shared code.
    - Otherwise, input data can be translated into a medium, which a shared class/worker implementation can use.

In other words - plugins have very little reason to interact with each other.
Thus, we will simply separate them as much as we can.

## VTubeStudio Preset
VTubeStudio preset has a goal of matching selected VTuber Model.
Each preset can only work with 1 expression and a list of hotkeys, using that expression.
Unless Hotkey component is added - preset can also read all hotkeys, and react to those which has a target expression.

As of v1.5, activation progress and microphone indicator will be listed in the main component.
Later, amongst configurable features, there will be:

- Keyword trigger (multiple types)
- Expression multiplier (increase progress gain depending on your facial expression)
- Better loudness interface (allows to smooth-out loudness peaks, working as a filter for a trigger)
- More plugins, if any will be needed.
- Plugin save files should have a versioning support, or allow JsonSerializer to deserialize settings without exceptions.

This preset will search for a first VTSConnection currently managing VTSModel with a matching ModelID.
Otherwise, it shows a warning near a model dropdown.
Model dropdown lists models found in all VTSConnections, but in a first version might default to just one.

## Warudo Preset
Warudo preset is WIP. Goals are not clear, but anything that will allow tracking a state of a specific animation,
as well as identifying a model with that specific animation, will do.

# Performance
Since now Plugin might be quite bulky - we can provide an option to disable UI entirely, leaving only application logic.
This can be a separate executable, or we can provide it as an option in the app itself.
We can remember this setting, and choose whether to initialize the window at all on the next session.
However, we still need to initialize a tray icon for the app, so user can close the program, or open it again.

Because of this, we need to assume that UI and Logic will be separated.
Useful WPF practices on this can be found amonst other amazing ideas and suggestions here:
https://metashapes.com/blog/not-shooting-foot-wpf-best-practices/
Only models (Services, etc.) and view-models (logic: RootViewModel, etc.) will persist.
View and Controls (MainWindow and components used on it) will not be initialized, or will be terminated when user selects performance mode.

This also means that ViewModels should only have the logic.
Animations, color selection, and any other UI-related info should be decided on a UI-layer.
Thus, performance on the UI layer is also important.

# Packets & API (VTS)
All Response packets should not have any required fields.
This is to make a few optimizations possible, if we were to pursue them.
However, Request packets should have required keywords, as it will prompt users making custom requests to initialize all properties.
This is important for avoiding exceptions, but might be changed if any of our target .NET version won't support it.