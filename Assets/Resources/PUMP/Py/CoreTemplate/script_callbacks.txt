import clr

reference_ex_logger = None

def add_reference(reference: str) -> None:
    try:
        clr.AddReference(reference)
    except:
        if reference_ex_logger is not None:
            reference_ex_logger(reference)


class OutputApplier:
    def __init__(self):
        self.callback_apply = None
        self.callback_apply_at = None
        self.callback_apply_to = None

    def set_callback(self, callback_apply, callback_apply_at, callback_apply_to) -> None:
        self.callback_apply = callback_apply
        self.callback_apply_at = callback_apply_at
        self.callback_apply_to = callback_apply_to
    
    def dispose(self) -> None:
        self.callback_apply = None
        self.callback_apply_at = None
        self.callback_apply_to = None

    def apply(self, values: list) -> None:
        if self.callback_apply is None:
            return
        self.callback_apply(values)

    def apply_at(self, index: int, value) -> None:
        if self.callback_apply_at is None:
            return
        self.callback_apply_at(index, value)

    def apply_to(self, name: str, value) -> None:
        if self.callback_apply_to is None:
            return
        self.callback_apply_to(name, value)


class Printer:
    def __init__(self) -> None:
        self.callback_print = None

    def set_callback(self, callback_print) -> None:
        self.callback_print = callback_print

    def dispose(self) -> None:
        self.callback_print = None

    def print(self, value: str) -> None:
        if self.callback_print is None:
            return
        self.callback_print(value)