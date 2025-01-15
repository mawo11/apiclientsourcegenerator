# Generator kodu klienta API

Rozwiązanie ma celu generowanie kodu klient HTTP w czasie kompilacji. Ma to na celu przyśpieszyć tworzenie klientów oraz generować najbardziej optymalny kod klienta. Klasa musi być partial. Każda publiczna metoda jest wpisywana w generowany interfejs.  Metody, które mają mieć wygenerowany kod muszą zostać zdefiniowane jako partial z odpowiednim atrybutami 

# Atrybuty klasy

ApiClientGenerator
| Atrybut | Opis |
| ------- | ---- |
| NetCore |  true - jeśli chcemy używać kodu per .NET |
| Serialization  | (Newtonsoft, SystemTextJson, Custom) - Globalna obsługa serializacji |
| ConnectionTooLongWarn = ms| - jeśli zostanie przekroczony czas to zostanie wywowała metoda private partial void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration) |

     
# Atrybuty metod
Get/Put/Post/Delete  - argument ścieżka w api
ThrowsExceptionsAttribute - jeśli metoda ma rzucać wyjątki
ConnectionTooLongWarn = ms jeśli zostanie przekroczony czas to zostanie wywowała metoda private partial void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration) 

Atrybuty parametrów
AliasAs(string) - nazwa parametru w query 
Body - wartość będzie przesyłana jak contentn json (Form = false, domyślnie ) lub jako form encoded(Form = true)
Fmt(string) - ciągu formatujący daną wartość, używane jest wywołanie metody ToString(...)
Header(string) wartość jest przesyłana jako nagłówek w zapytaniu 
