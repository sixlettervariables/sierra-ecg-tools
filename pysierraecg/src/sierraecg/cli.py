#!/usr/bin/env python3

import colorama
import typer
from typer import Argument

from sierraecg.lib import read_file


def main(n: str = Argument(..., help="The input n of fact(n)")) -> None:
    """Compute factorial of a given input."""
    colorama.init(autoreset=True, strip=False)

    f = read_file(n)

    print(
        f"sierraecg({colorama.Fore.CYAN}{n}{colorama.Fore.RESET}) = "
        f"{colorama.Fore.GREEN}{f.doc_type} {f.doc_ver}{colorama.Fore.RESET}"
    )


# Allow the script to be run standalone (useful during development).
if __name__ == "__main__":
    typer.run(main)
