# Defines the node's name
name: str = "ScriptingNode"
# Specifies number of input ports
input_counts: int = 2
# Specifies number of output ports
output_counts: int = 1
# Object responsible for applying output signals to the node
output_applier: OutputApplier = None

def init() -> None:
    """
    Initialization function called when the node is freshly created or during Undo/Redo operations.
    Keep it clean, keep it lean.
    """
    pass

def state_update(inputs: list, index: int, state: bool, is_changed: bool) -> None:
    
    """
    The nerve center - triggered whenever any signal is detected on input ports.
    
    Parameters:
        inputs (list): Boolean list representing the state of each input port
        index (int): Index of the input port that just changed. -1 when state_update is triggered by system
        state (bool): The new state value (True/False) of the modified port
        is_changed (bool): Flag indicating if the value actually changed from previous state
    """
    pass