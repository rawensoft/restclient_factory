# RestClient Factory

Эта библиотека должна помочь в получении нужного RestClient по прокси. Сделана она [по рекомендациям Microsoft](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines#recommended-use). Случая когда она может пригодиться:
- Нужно связать библиотека несколько библиотек в одну
- Использование одного пуля соединения с прокси для доступа к сайтам

## Почему `RestClient`, а не `HttpClient`?
На это повлияло несколько причин, самые главные:
- Можно отключить cookie в `HttpClient`, а `RestClient` в этот момент будет их парсить, тогда можно использовать один `RestClient` для работы с каждым запросом и они не будут друг другу мешать, например в одном `CookieContainer` будет содержаться данные со всех запросов
- Простота и быстрота написания кода
- Лёгкость использования

## Как использовать?
Пример выполнения обычного запроса требующего куки и прокси:
```
public static RestResponse Download(string url, IWebProxy? proxy, CookieContainer cookie, Method method)
{
	var uri = new Uri(url);
	var client = RestClientFactory.Factory.GetClient(proxy, uri);
	var request = new RestRequest(uri, method)
	{
		CookieContainer = cookie
	};
    var response = client.Execute(request);
    return response;
}
```

## Как изменить конфигурирование RestClient?
Это можно сделать через `RestClientFactory.SetFuncConfigureClient`, но нужно придерживаться некоторых рекомендаций:
- FollowRedirects нужно выключить и обрабатывать их самостоятельно, иначе может быть `loop` обработки этих запросов
- Если нужен один UserAgent для всех запросов, то нужно установить его в `RestClientOptions.UserAgent`, иначе поставить туда `null` и в каждом запросе вставлять его через `RestRequest.AddHeader(KnownHeaders.UserAgent, UserAgent)`
- Установить прокси в `RestClientOptions.Proxy` и в `RestClientOptions.ConfigureMessageHandler => h.Proxy` и поставить `RestClientOptions.ConfigureMessageHandler => h.UseProxy = true`
- Если не нужен один CookieContainer для всех запросов, то нужно поставить `RestClientOptions.CookieContainer = null`, `RestClientOptions.ConfigureMessageHandler => h.CookieContainer = new()` и поставить `RestClientOptions.ConfigureMessageHandler => h.UseCookies = false` тогда куки можно будет установить для каждого запроса через `RestRequest.CookieContainer`, а получить через `RestResponse.Cookies`

## Зависимости
Для работы нужны:
- [RestSharp 110.2.0](https://www.nuget.org/packages/RestSharp/110.2.0) - для обработки запросов
- [System.Collections.Concurrent 4.3.0](https://www.nuget.org/packages/System.Collections.Concurrent/4.3.0) - для потокобезопасной работы со списком всех `RestClient`
- .NET 7
