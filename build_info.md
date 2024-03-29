# Building Chess.NET
Chess.NET is built using Unity and the Mono framework and can therefore be built for multiple targets. To achieve this, follow these instructions:
### Opening the project
1. Install Unity Hub (skip ahead if already installed)
2. Select Installs -> Install Editor -> 2022 LTS (2022.3.9f1 at the time of writing)
3. Install the toolchain relevant to your OS/architecture. It is recommended Visual Studio is installed as well.
4.  Select Projects -> Add. Open the cloned repo folder.
5. Upon launch, the Unity package manager should fetch the needed dependencies. This will take some time.
6. Go to Scenes -> Chessboard, open the Scene and check for any compile errors.
### Building cobra 
Before the project can be built, the cobra engine must be built. cobra is built in Python 3 and uses PyInstaller to package the python project into an executable. 
1. Clone the cobra submodule if it is not already present. 
2. Ensure python3 is installed.
3. Open a terminal session at Chess.NET-main/cobra
4. Create a virtual environment and install the dependencies by inputting the following:

**macOS/Linux**
```
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```
**Windows**
```
python3 -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```
5. Confirm the requirements have been satisfied by running the engine in standalone mode.
```
cd src
python3 -m main --use-default-settings
```
6. Download the [Stockfish binary](https://stockfishchess.org/download) for your OS/architecture. Place the binary in the /src folder
7. Package cobra into an executable by inputting the following
```
pyinstaller -F --add-binary "file_name" -n cobra main.py
```
where:
"file_name" (with quotations) is the name of the compiled binary. 
-F flag creates one file
-n takes in the name (cobra)

8. Drag the packaged executable into the StreamingAssets folder. Any changes to the name of the cobra executable must be updated in Communication.Client.CreateEngineProcess().
9. Build the executable by going to File -> Build Settings. Select the target for the compiler.
10. The solution should be built.


