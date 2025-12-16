<div align="center">

# FlowLab

[![Unity](https://img.shields.io/badge/Unity-6000.2.6f2-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-.NET%20Framework-512BD4?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![IronPython](https://img.shields.io/badge/IronPython-3.4.2-3776AB?logo=python)](https://ironpython.net/)

[🚀 Getting Started](#-getting-started) · [⌨️ Shortcuts](#️-keyboard-shortcuts) · [📦 Node Guide](#-node-guide) · [🐍 Scripting](#-scriptingnode) · [⚙️ Settings](#️-settings)

🌐 [한국어](README_ko.md)

</div>

---

## 🚀 Getting Started

### Basic Workflow

```
1. Place Nodes  →  2. Connect Ports  →  3. See It Run
```

### 1. Placing Nodes

Press `Tab` to open the **Node Palette**.

Drag a node to place it in the workspace.

### 2. Connecting Nodes

Drag from a node's **output port** (right side) to another node's **input port** (left side).

> 💡 **Tip:** Only ports of the same type can be connected. (Types are distinguished by port color)

### 3. Execution

Once connected, data flows automatically. Click a Trigger node or change input values to see results update in real-time.

---

## ⌨️ Keyboard Shortcuts

### Basic Controls

| Function | Shortcut | Description | Configurable |
|----------|----------|-------------|:------------:|
| **Open Node Palette** | `Tab` | Display node list | ✓ |
| **Open Save Panel** | `S` | Save/Load | ✓ |
| **Open Console** | `` ` `` | Debug console | ✓ |
| **Toggle Minimap** | `M` | Map overview | ✓ |

### Editing

| Function | Shortcut | Configurable |
|----------|----------|:------------:|
| **Undo** | `Ctrl + Z` | ✓ |
| **Redo** | `Ctrl + Shift + Z` | ✓ |
| **Copy** | `Ctrl + C` | ✓ |
| **Paste** | `Ctrl + V` | ✓ |
| **Cut** | `Ctrl + X` | ✓ |
| **Select All** | `Ctrl + A` | ✓ |
| **Delete Selection** | `Delete` | ✓ |
| **Disconnect** | `Backspace` | ✓ |

### View Controls

| Function | Control |
|----------|---------|
| **Area Selection** | Left-click drag |
| **Pan** | Right-click drag / Click minimap |
| **Vertical Scroll** | Mouse wheel |
| **Horizontal Scroll** | `Shift + Wheel` |
| **Zoom In** | `Ctrl + Wheel ↑` |
| **Zoom Out** | `Ctrl + Wheel ↓` |

### Snap Mode

| Function | Shortcut | Configurable |
|----------|----------|:------------:|
| **Toggle Snap Mode** | `Q` | ✓ |

> When snap mode is enabled, line edges are automatically aligned at right angles.

---

## 📦 Node Guide

### Data Types

FlowLab supports 5 data types, distinguished by port color.

| Type | Description | Example |
|------|-------------|---------|
| ![Bool](https://img.shields.io/badge/Bool-blue) | True/False | `true`, `false` |
| ![Int](https://img.shields.io/badge/Int-green) | Integer | `0`, `42`, `-100` |
| ![Float](https://img.shields.io/badge/Float-orange) | Decimal | `3.14`, `0.5` |
| ![String](https://img.shields.io/badge/String-pink) | Text | `"Hello"` |
| ![Pulse](https://img.shields.io/badge/Pulse-white) | Execution signal | Event trigger |

### Node Categories

#### Logic (10 nodes)
Performs logical operations.

| Node | Description |
|------|-------------|
| AND | True when all inputs are true |
| OR | True when any input is true |
| XOR | True when inputs differ |
| NOT | Inverts input |
| All | Checks if all inputs are true |
| Any | Checks if any input is true |

#### Flow (4 nodes)
Controls program flow.

| Node | Description |
|------|-------------|
| If | Branches based on condition |
| While | Loops while condition is true |
| Branch | Multiple branching |
| Select | Selects based on value |

#### I/O (7 nodes)
Handles user input/output.

| Node | Description |
|------|-------------|
| Trigger | Emits Pulse signal on click |
| InputField | Text/number input |
| Display | Shows values |

#### Signal (17 nodes)
Distributes and merges signals.

| Node | Description |
|------|-------------|
| Splitter | Distributes one signal to multiple outputs |
| Merger | Merges multiple signals into one |
| SignalDetector | Detects signal changes |

#### Math (30 nodes)
Performs mathematical operations.

| Node | Description |
|------|-------------|
| Add, Subtract, Multiply, Divide | Arithmetic operations |
| Sin, Cos, Tan, Atan | Trigonometric functions |
| Lerp | Linear interpolation |
| Clamp | Limits value range |

#### Util (5 nodes)
Provides utility functions.

| Node | Description |
|------|-------------|
| StringLength | String length |
| StringReplace | String replacement |

#### Advanced (3 nodes)
Advanced extension features.

| Node | Description |
|------|-------------|
| ScriptingNode | Runs Python scripts |
| ClassedNode | Encapsulates circuits as modules |

---

## 🐍 ScriptingNode

Create custom nodes with Python scripts.

### Creating a Template

**Right-click** on ScriptingNode → Select **Create Template** from the context menu

A Python script file with the basic structure will be generated.

### Basic Template

```python
# Node name
name: str = "My Custom Node"

# Input port settings
input_list: list = ['input1', 'input2']
input_types: list = [float, float]

# Output port settings
output_list: list = ['result']
output_types: list = [float]

# Async mode (set True for long-running tasks)
is_async: bool = False

# System-injected objects (do not modify)
output_applier: OutputApplier = None
printer: Printer = None

# Lifecycle functions
def init(inputs: list) -> None:
    """Called when node is initialized"""
    pass

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """Called when input changes"""
    a = inputs[0]
    b = inputs[1]
    
    if a is not None and b is not None:
        output_applier.apply([a + b])

def terminate() -> None:
    """Called when node is deleted"""
    pass
```

### Available Types

| Type | Python | Example |
|------|--------|---------|
| Bool | `bool` | `True`, `False` |
| Int | `int` | `42` |
| Float | `float` | `3.14` |
| String | `str` | `"Hello"` |
| Pulse | `Pulse` | `Pulse()` |

### Output Control API

```python
# Set all outputs at once
output_applier.apply([value1, value2, value3])

# Set output at specific index
output_applier.apply_at(0, value)

# Set output by name
output_applier.apply_to('result', value)

# Signal loss (pass None)
output_applier.apply([True, None, 3.14])
```

### Python Standard Library

Most Python standard library modules are available.

```python
import json
import datetime
import collections

data = {"name": "FlowLab", "version": 1.0}
json_str = json.dumps(data)
parsed = json.loads(json_str)
```

### .NET Library Access

```python
# Add assembly reference
add_reference('System')

# Import namespaces
from System.Net import WebClient
from System.Threading import Thread

# HTTP request example
client = WebClient()
response = client.DownloadString("https://api.example.com/data")
```

### Example: HTTP API Call

```python
name: str = "API Caller"
input_list: list = ['url', 'trigger']
input_types: list = [str, Pulse]
output_list: list = ['response', 'status']
output_types: list = [str, bool]
is_async: bool = True  # Use async for network requests!

output_applier: OutputApplier = None
printer: Printer = None

add_reference('System')
from System.Net import WebClient

def state_update(inputs, index, state, before_state, is_changed):
    if index == 1 and inputs[0] is not None:  # On trigger input
        try:
            client = WebClient()
            response = client.DownloadString(inputs[0])
            output_applier.apply([response, True])
            printer.print("Success!")
        except Exception as e:
            output_applier.apply([str(e), False])
            printer.print("Error: " + str(e))
```

---

## 📦 ClassedNode

Create reusable nodes by encapsulating complex circuits.

### How to Use

1. **Design Circuit**: Build your desired functionality with regular nodes
2. **Place External I/O**: Define external input/output ports
3. **Convert to ClassedNode**: Encapsulate the entire circuit as a single node
4. **Save/Share**: Export as `.lcm` file

### Save and Load

| Action | Method |
|--------|--------|
| **Save** | Save/Load Panel → Export → Select save → Enter filename |
| **Load** | Save/Load Panel → Import → Select .lcm file |

> 💡 Share `.lcm` files with teammates to use the same custom nodes.

---

## ⚙️ Settings

### Opening Settings

Hover over the top-left corner of the screen to reveal the toolbar.

Click the `?` button → Select `Option`

### Simulation Speed

| Mode | Description |
|------|-------------|
| **Frame** | Signal propagates per frame |
| **Fixed Time** | Propagates at specified interval (0.01 ~ 10 sec) |
| **⚡ Immediately** | Instant propagation (best performance) |

#### Immediately Mode

When checked, the **Max Iterations Per Frame** setting appears.

A **feedback loop** occurs when outputs connect back to inputs, like Node A → Node B → Node A. This can cause infinite signal repetition. This value sets how many times the same connection can be traversed within a single frame. If exceeded, processing automatically continues in the next frame.

> 💡 For typical circuits, the default value (5) is recommended.

### Key Mapping

You can customize shortcuts in the settings screen.

Configurable items:
- Undo / Redo
- Copy / Cut / Paste
- ToggleSnapMode
- OpenPalette
- OpenSaveLoadPanel
- SelectAll / SelectDelete

<div align="center">

</div>
