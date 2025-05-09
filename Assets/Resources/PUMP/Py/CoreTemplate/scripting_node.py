# ===============================================================
# IronPython Version: 3.4.2 (3.4.2.1000)
# ===============================================================

# ==================== Available Python Modules =================
# System/Utilities: sys, time, gc, atexit, itertools, marshal, signal
# Math/Computation: math, cmath, _random, _heapq, _bisect
# String/Text Processing: re, _string, _sre, _struct, _csv
# Data Structures: array, _collections
# File/IO: _io, zipimport, _bz2
# Networking: _socket, _ssl, _overlapped
# Windows Specific: msvcrt, winreg, winsound, _winapi, nt
# ===============================================================

# ===================== .NET Framework Access ===================
# All .NET Framework libraries are accessible through IronPython
# Default references: System, System.Net

# To add additional references:
# add_reference('required_assembly')
# from required_namespace import required_class

# Example:
# add_reference('System')
# add_reference('System.Threading')
# add_reference('System.Net')
#
# import System
# from System.Net import WebClient
# from System.Threading import Thread, ThreadStart

# Key useful .NET namespaces:
# - System: Basic classes, data types, utilities
# - System.IO: File and directory operations
# - System.Net: Network communications, HTTP requests
# - System.Threading: Threads, timers, synchronization
# - System.Collections: Collections, lists, dictionaries
# - System.Text: String processing, encoding
# ===============================================================



# <<Node Configuration>>

# Defines the node's name
# ※This value is only reflected in the node when initially set; changes within the function have no effect
name: str = "Scripting Node"

# Set the number and names of input ports with the list below
# ※This value is only reflected in the node when initially set; changes within the function have no effect
input_list: list = ['in 1', 'in 2']

# Set the number and names of output ports with the list below
# ※This value is only reflected in the node when initially set; changes within the function have no effect
output_list: list = ['out 1']

# When True, allows this Node's methods to be executed asynchronously (but terminate() is always executed synchronously)
# ※This value is only reflected in the node when initially set; changes within the function have no effect
is_async: bool = False

# Controls whether state_update is automatically called after initialization
# When True, system will call state_update once after init() with (index=-1, state=False, is_changed=False)
# ※This value is only reflected in the node when initially set; changes within the function have no effect
auto_state_update_after_init: bool = False



# <<Node Controllers>>

# ====================== WARNING ======================
# DO NOT MODIFY THE FOLLOWING SYSTEM VARIABLES
# These are automatically set by the system and will be overwritten

# Object that controls output ports
# Available API: def apply(self, outputs: list) -> None:
# You need to provide a bool list as input. ※The length of this list must match output_counts
output_applier: OutputApplier = None

# Printer object
# Available API: def print(self, value: str) -> None:
# Used to display string information on the node's display.
printer: Printer = None
# =====================================================



# <<Node Lifecycle Methods>>

def init(inputs: list) -> None:
    """
    Initialization function called when the node is freshly created or during Undo/Redo operations
    Suitable for setting the initial state of output ports
    Keep it clean, keep it lean

    Parameters:
        inputs (list): Boolean list representing the state of each input port. Read only; do not modify element values
    """
    pass

def terminate() -> None:
    """
    Cleanup function called when the node is deleted or during Undo/Redo operations
    Dispose of any resources that need to be cleaned up
    """
    pass

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    """
    The nerve center - triggered whenever any signal is detected on input ports
    Not all parameters need to be used. In most cases, only inputs is necessary
    However, if you want to know the type of "port that received a signal", its state, and the difference from its previous state, use the remaining parameters
    
    Parameters:
        inputs (list): Boolean list representing the state of each input port
        index (int): Index of the input port that just changed. -1 when state_update is triggered by system
        state (bool): The new state value (True/False) of the modified port
        is_changed (bool): A flag indicating whether the state of the changed port is different from its previous state
    """
    pass