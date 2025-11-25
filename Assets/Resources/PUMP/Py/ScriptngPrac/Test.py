# ===============================================================
# deque 테스트 노드
# ===============================================================

from collections import deque
import abc           # 추상 베이스 클래스
import copy          # 깊은/얕은 복사
import enum          # 열거형
import functools     # 고차 함수
import itertools     # 이터레이터 도구
import operator      # 연산자 함수
import types         # 타입 관련
import typing        # 타입 힌트

import os            # 운영체제 인터페이스
import pathlib       # 객체지향 경로
import glob          # 파일 패턴 매칭
import shutil        # 파일 작업
import tempfile      # 임시 파일
import filecmp       # 파일 비교
import fileinput     # 파일 입력
import fnmatch       # 파일명 매칭

import collections   # deque, Counter, OrderedDict 등
import heapq         # 힙 큐
import bisect        # 이진 탐색
import queue         # 큐
import weakref       # 약한 참조

name: str = 'Deque Test'

input_list: list = ['▶ Push', 'Value', '▶ Pop', '▶ Clear']
output_list: list = ['Size', 'Front', 'Back', 'Is Empty']

input_types: list = [Pulse, str, Pulse, Pulse]
output_types: list = [int, str, str, bool]

is_async: bool = False

output_applier: OutputApplier = None
printer: Printer = None
json_util: JsonUtil = JsonUtil()

# deque 인스턴스
my_queue = None

def init(inputs: list) -> None:
    global my_queue
    my_queue = deque(maxlen=10)  # 최대 10개 항목
    update_outputs()
    printer.print("Deque initialized (max: 10)")

def terminate() -> None:
    global my_queue
    my_queue = None
    printer.print("Deque terminated")

def update_outputs():
    """출력 포트 업데이트"""
    size = len(my_queue)
    front = my_queue[0] if my_queue else "Empty"
    back = my_queue[-1] if my_queue else "Empty"
    is_empty = len(my_queue) == 0
    
    output_applier.apply([size, front, back, is_empty])

def state_update(inputs: list, index: int, state, before_state, is_changed: bool) -> None:
    global my_queue
    
    # Push 신호
    if index == 0 and state is not None:
        value = inputs[1] if inputs[1] is not None else "None"
        my_queue.append(value)
        printer.print(f"Pushed: {value}")
        update_outputs()
    
    # Pop 신호
    elif index == 2 and state is not None:
        if my_queue:
            popped = my_queue.popleft()
            printer.print(f"Popped: {popped}")
        else:
            printer.print("Queue is empty!")
        update_outputs()
    
    # Clear 신호
    elif index == 3 and state is not None:
        my_queue.clear()
        printer.print("Queue cleared")
        update_outputs()
    
    # Value 변경 (정보만 표시)
    elif index == 1:
        if state is not None:
            printer.print(f"Ready to push: {state}")