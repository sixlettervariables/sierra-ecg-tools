from typing import List

import numpy as np
import numpy.typing as npt


def xli_decode(data: bytes, labels: List[str]) -> List[npt.NDArray]:
    return [np.array([], dtype=np.int16) for _ in range(len(labels))]
