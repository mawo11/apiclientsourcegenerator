# API Client Code Generator

The solution aims to generate HTTP client code at compile time. The aim is to speed up the creation of clients and generate the most optimal client code. The class must be partial. Every public method is written into the generated interface.Methods that are to have generated code must be defined as partial with the appropriate attributes.

# Class attributes

ApiClientGenerator

| Attribute | Description | 
| ------- | ---- |
| NetCore | true - if we want to use the code per .NET |
| Serialization | (Newtonsoft, SystemTextJson, Custom) - Global serialization support |
| ConnectionTooLongWarn(int) | Timeout in ms after which the LogConnectionTooLongWarning method will be invoked. For all methods in the class |

# Method Attributes

| Attribute | Description | 
| ------- | ---- |
| Get(string) | URL resource, HTTP Get method |
| Post(string) | URL resource, HTTP Post method |
| Put(string)  | URL resource, HTTP Put method  |
| Delete(string)  | URL resource, HTTP Delete method  |
| ThrowsExceptionsAttribute | If provided, the method will pass exceptions after calling the error logging method|
| ConnectionTooLongWarn(int) | Timeout in ms after which the method LogConnectionTooLongWarning will be invoked.|

# Parameter Attributes

| Attribute | Description | 
| ------- | ---- |
| Context:   | CAliasAs(string) | parameter name in query |  \nText to translate: | CAliasAs(string) | parameter name in query |
| Body | the value will be sent as JSON content (Form = false, by default) or as form encoded (Form = true) |
| Fmt(string) | a string formatting the given value, the ToString(...) method is used |
| Header(string) | the value is sent as a header in the request |

# Private methods required for implementation

* Method enabling error logging<br/>
    - private partial void LogError(string methodName, string path, System.Exception ex) 
    - private partial  void LogError(string methodName, string path, string message)
* A method that allows logging of extended method execution time if the ConnectionTooLongWarn attribute is defined on any method or globally in the ApiClientGenerator attribute.
    - private partial  void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration) - 
