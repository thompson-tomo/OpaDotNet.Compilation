# Changelog

## OpaDotNet.Compilation v1.2.2 (2023-09-18)

### Bug Fixes

* Interop compiler is not removing temporary artifacts ([a52ac06](https://github.com/me-viper/OpaDotNet.Compilation/commit/a52ac06617e0dbd627197a38d2e158efe963caa3))

## OpaDotNet.Compilation v1.2.1 (2023-09-18)

### Bug Fixes

* Interop compiler is not removing temporary artifacts ([a52ac06](https://github.com/me-viper/OpaDotNet.Compilation/commit/a52ac06617e0dbd627197a38d2e158efe963caa3))

## OpaDotNet.Compilation v1.2.0 (2023-08-23)

### Bug Fixes

* Support building bundle from bundle in Cli compiler ([6b17a56](https://github.com/me-viper/OpaDotNet.Compilation/commit/6b17a5619320f1c78bfff49f726937a9ea91665a))
* Normalize path handling for bundles ([1987a1d](https://github.com/me-viper/OpaDotNet.Compilation/commit/1987a1d13a328e37bbe7a6ae1bfbbe9de128a43f))

### Features

* Implement API to construct bundles ([5425be1](https://github.com/me-viper/OpaDotNet.Compilation/commit/5425be1a25200f690ba1fcc27edf73c1ce8fa38d))
* Support compilation from bundle Stream ([310c92f](https://github.com/me-viper/OpaDotNet.Compilation/commit/310c92feed48e0d1704799efaa2a34d1005c1aed))
* Improve interop compiler ([e6d0a5e](https://github.com/me-viper/OpaDotNet.Compilation/commit/e6d0a5e469c4e7cbfeff2790f90375075bf7cc32))
* Support more compiler flags ([6381238](https://github.com/me-viper/OpaDotNet.Compilation/commit/6381238585147f05ef98d285f354810c2bb9ac03))

## OpaDotNet.Compilation v1.1.3 (2023-08-18)

### Bug Fixes

* Do bundle path normalization for interop compiler
* Do source file path normalization for interop compiler

## OpaDotNet.Compilation v1.1.1 (2023-08-17)

### Bug Fixes

* Fix entrypoints array construction

## OpaDotNet.Compilation v1.1.0 (2023-08-17)

### Bug Fixes

* Fix invalid interop call
* Fix nuget package health warnings

### Code Refactoring

* Improve naming. Support future extensions

### BREAKING CHANGES

* Native interface have been changed
