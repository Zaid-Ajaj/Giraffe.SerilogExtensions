#### 2.2.0 - 2023-02-08
* Add ability to ignore request headers with Authorization and Cookie ignored by default

#### 2.1.0 - 2021-08-05
* Update to Giraffe v6

#### 2.0.0 - 2021-06-29
* Update to Giraffe v5

#### 1.3.0 - 2019-09-09
* Fix the order of the logged `FullPath` and fill it with just the `Path` when query string parameters are not provided

#### 1.2.0 - 2019-06-04
* Re-use `RequestId` from context when logging in Saturn

#### 1.1.2 - 2019-01-26
* Use the correct ignored fields in the ResponseLogEnrichrer

#### 1.1.1 - 2019-01-26
* Fixes #1 casting content length to a log event property now works

#### 1.1.0 - 2018-12-23
* Ensure correct status code is returned on for unhandled exceptions

#### 1.0.0 - 2018-12-22
* Initial release
