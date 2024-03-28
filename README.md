# Chess.NET
### **A Chess frontend created in Unity**

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
- cobra executable 
