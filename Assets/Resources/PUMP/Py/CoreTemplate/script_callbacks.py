class OutputApplier:
    def __init__(self):
        self.callback = None

    def set_callback(self, callback) -> None:
        self.callback = callback
    
    def dispose(self) -> None:
        self.callback = None

    def apply(self, outputs: list) -> None:
        if self.callback is None:
            return
        
        self.callback(outputs)

class Printer:
    def __init__(self) -> None:
        self.callback = None

    def set_callback(self, callback) -> None:
        self.callback = callback

    def dispose(self) -> None:
        self.callback = None

    def print(self, value: str) -> None:
        if self.callback is None:
            return
        
        self.callback(value)