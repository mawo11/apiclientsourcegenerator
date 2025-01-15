# Generator kodu klienta API

Rozwiązanie ma celu generowanie kodu klient HTTP w czasie kompilacji. Ma to na celu przyśpieszyć tworzenie klientów oraz generować najbardziej optymalny kod klienta. Klasa musi być partial. Każda publiczna metoda jest wpisywana w generowany interfejs.  Metody, które mają mieć wygenerowany kod muszą zostać zdefiniowane jako partial z odpowiednim atrybutami 

# Atrybuty klasy

ApiClientGenerator
| Atrybut | Opis |
| ------- | ---- |
| NetCore |  true - jeśli chcemy używać kodu per .NET |
| Serialization  | (Newtonsoft, SystemTextJson, Custom) - Globalna obsługa serializacji |
| ConnectionTooLongWarn(int) |  Timeout w ms po przekroczeniu którego zostanie wywowałana metoda LogConnectionTooLongWarning. Dla wszystkich metod w klasie  |

     
# Atrybuty metod
| Atrybut | Opis |
| ------- | ---- |
| Get(string) | Zasób url, Metoda HTTP Get |
| Post(string)  | Zasób url, Metoda HTTP Post  |
| Put(string)  | Zasób url, Metoda HTTP Put  |
| Delete(string)  | Zasób url, Metoda HTTP Delete  |
| CThrowsExceptionsAttribute | Jeśli jest podany, metoda będzie przekazywać wyjątki po uprzedniu wywołaniu metody do logowania błędów |
| ConnectionTooLongWarn(int) |  Timeout w ms po przekroczeniu którego zostanie wywowałana metoda LogConnectionTooLongWarning. |

# Atrybuty parametrów
| Atrybut | Opis |
| ------- | ---- |
| CAliasAs(string) | nazwa parametru w query |
| CBody | wartość będzie przesyłana jak contentn json (Form = false, domyślnie ) lub jako form encoded(Form = true)|
| CFmt(string) | ciągu formatujący daną wartość, używane jest wywołanie metody ToString(...)|
| CHeader(string) | wartość jest przesyłana jako nagłówek w zapytaniu |

# Metody prywatne wymagane do implementacji 
private partial void LogError(string methodName, string path, System.Exception ex) - Metoda umożliwiająca logowanie błędów 
private partial void LogConnectionTooLongWarning(string methodName, string path, long connectionDuration) - metoda umożliwiająca logowania wydłużonego czasu działania metody 
