# Chess.NET
### **A Chess frontend created in Unity**
![Sequence 01](https://github.com/george-m2/Chess.NET/assets/60574716/3a77e5ce-1f85-4426-8c80-cba2dbb0735d)

#### Features:
- Smooth piece movement via clicking and dragging
- Move highlighting
- Cursor highlighting
- Abilty to undo/redo moves
- PGN exporting
- AI opponent, either via the 'cobra' engine or Stockfish 16
- Best move/blunder analysis via cobra/Stockfish
- ACPL analysis

#### Requirements:
- Unity 2022.3.9f1.
- [UnityMainThreadDispatcher](https://github.com/PimDeWitte/UnityMainThreadDispatcher) - used for thread safety when communicating between cobra and Chess.NET.
- NetMQ 4.0+ - used for communciation with the cobra engine.
- NuGetForUnity (optional, but highly recommended)

To build Chess.NET see [the build instructions](https://github.com/george-m2/Chess.NET/blob/9a5711b76820567a96a2529c9df60ba4c9a560cc/build_info.md).
