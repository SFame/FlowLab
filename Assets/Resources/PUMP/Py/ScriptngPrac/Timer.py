add_reference("System")
from System.Threading import Thread, ThreadInterruptedException

# Node name configuration - Name displayed in the visual editor
name: str = "Async Timer"

# Input port configuration - Define 4 input ports
# Trig: Timer start trigger, Rst: Timer reset, Inc: Time increase, Clr: Time clear
input_list: list = ['Trig', 'Rst', 'Inc', 'Clr']

# Output port configuration - Define 1 output port
# Out: Outputs True signal when timer completes
output_list: list = ['Out']

# Input port type configuration - All inputs are bool type (True/False signals)
input_types: list = [bool, bool, bool, bool]

# Output port type configuration - Output is bool type (True/False signal)
output_types: list = [Pulse]

# Enable asynchronous execution - run_timer() function executes in separate thread
is_async: bool = True

# System-managed output controller object - Used to send values to output ports
output_applier: OutputApplier = None

# System-managed display object - Used to show text above the node
printer: Printer = None

# Global variable to track timer execution state
is_timer_running = False
# Target timer duration (in seconds)
target_time = 1.0
# Time increment unit when Inc button is clicked
time_step = 0.5
# Timer update interval - remaining time is displayed at this frequency
update_interval = 0.1

def run_timer():
    """
    Function that executes the actual timer logic
    Runs asynchronously to avoid blocking other node operations
    """
    global is_timer_running
    
    try:
        # Handle immediate completion when timer time is 0 or less
        if target_time <= 0:
            printer.print("Timer time is zero!")  # Display message above node
            output_applier.apply([Pulse()])           # Send True signal to output port
            Thread.Sleep(100)                      # Wait 0.1 seconds
            is_timer_running = False
            return
            
        # Set timer to running state
        is_timer_running = True
        printer.print(f"Timer started: {target_time:.1f}s")  # Display start message
        
        # Countdown logic
        remaining_time = target_time
        update_count = int(target_time / update_interval)  # Calculate number of updates
        
        # Execute timer progression step by step
        for i in range(update_count):
            if not is_timer_running:
                return  # Exit immediately if timer was stopped elsewhere
                
            # Calculate and display remaining time
            remaining_time = target_time - (i * update_interval)
            printer.print(f"Time left: {remaining_time:.1f}s")
            
            # Wait for specified interval (convert to milliseconds)
            Thread.Sleep(int(update_interval * 1000))
        
        # Handle remaining time (non-divisible portion)
        last_interval = target_time - (update_count * update_interval)
        if last_interval > 0 and is_timer_running:
            Thread.Sleep(int(last_interval * 1000))
        
        # Timer completion handling
        if is_timer_running:
            printer.print("Timer completed!")
            
            # Send completion signal to output port (True pulse)
            output_applier.apply([Pulse()])
            
            # Change to False after short pulse signal
            Thread.Sleep(100)
            if is_timer_running:
                printer.print("Ready for next trigger")
                
    except ThreadInterruptedException:
        # Handle forced thread interruption
        printer.print("Timer interrupted")
    except Exception as e:
        # Display error message for other exceptions
        printer.print(f"Error: {str(e)}")
    finally:
        # Cleanup code that always executes
        is_timer_running = False

def init(inputs: list) -> None:
    """
    Initialization function called when node is created or during Undo/Redo
    inputs: List of current input port states [Trig, Rst, Inc, Clr]
    """
    global is_timer_running, target_time
    is_timer_running = False
    target_time = 1.0  # Initialize to default timer duration
    
    # Initialize output port to False
    # Display initial state message
    printer.print(f"Timer ready: {target_time:.1f}s")

def terminate() -> None:
    """
    Cleanup function called when node is deleted or during Undo/Redo
    Safely stops any running timer
    """
    global is_timer_running
    is_timer_running = False  # Force stop any running timer

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    """
    Main logic function called whenever signals arrive at input ports
    
    inputs: Current state of all input ports [Trig, Rst, Inc, Clr]
    index: Index of input port where change occurred (0:Trig, 1:Rst, 2:Inc, 3:Clr)
    state: New state value of the changed port
    is_changed: Whether the value is different from previous state
    is_disconnected: Whether the change was caused by disconnection
    """
    global is_timer_running, target_time
    
    # Ignore if no change or signal changed to False (only process Rising Edge)
    if not is_changed or state is None or (index >= 0 and not state):
        return
    
    # Handle each input port separately
    if index == 0 and state:  # Trigger signal (Port 0)
        if not is_timer_running:
            # Start new timer only when not already running
            run_timer()  # Start timer asynchronously
            
    elif index == 1 and state:  # Reset signal (Port 1)
        # Stop running timer and reset output
        is_timer_running = False
        output_applier.apply([None])
        printer.print(f"Timer reset: {target_time:.1f}s")
            
    elif index == 2 and state:  # Increase Time signal (Port 2)
        if not is_timer_running:  # Allow time adjustment only when timer is not running
            target_time += time_step  # Increase time by configured unit
            printer.print(f"Timer set to: {target_time:.1f}s")
            
    elif index == 3 and state:  # Clear Time signal (Port 3)
        if not is_timer_running:  # Allow time adjustment only when timer is not running
            target_time = 0.0  # Reset timer duration to 0
            printer.print("Timer cleared to 0.0s")