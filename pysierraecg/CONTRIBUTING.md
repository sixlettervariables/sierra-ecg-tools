# Contributing to pysierraecg
All contributions to the project will be considered for inclusion, simply submit a pull request!

1. [Fork](https://github.com/sixlettervariables/sierra-ecg-tools/fork) the repository ([read more detailed steps](https://help.github.com/articles/fork-a-repo)).
2. [Create a branch](https://help.github.com/articles/creating-and-deleting-branches-within-your-repository#creating-a-branch)
3. Make and commit your changes
4. Push your changes back to your fork.
5. [Submit a pull request](https://github.com/sixlettervariables/sierra-ecg-tools/compare/) ([read more detailed steps](https://help.github.com/articles/creating-a-pull-request)).

## Bootstrapping
To bootstrap the environment:

1. Create a virtual environment and activate it.
2. `pip install -r bootstrap-requirements.txt`
3. `pip-compile --extra=dev --output-file=dev-requirements.txt pyproject.toml`
4. `pip-compile --output-file=requirements.txt pyproject.toml`
5. Optionally: `pip install -r dev-requirements.txt`
6. Optionally: `pip install -r requirements.txt`

You can now run `tox`.
