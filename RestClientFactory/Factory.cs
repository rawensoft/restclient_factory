using RestSharp;
using System.Collections.Concurrent;
using System.Net;

namespace RestClientFactory;

public static class Factory
{
	private static readonly ConcurrentDictionary<int, RestClient> _clients = new(4, 1000);
	private static Func<IWebProxy?, RestClient> _funcGetClient = DefaultConfigureClient;

	/*
	 * Про правила использования HttpClient можно прочитать здесь:
	 * https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-7.0#consumption-patterns
	 * https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
	 * 
	 * Именно поэтому один HttpClient должен работать для одного домена или с одним прокси
	 * RestSharp помогает с обработкой куков запроса, поэтому в HttpClientHandler мы ставим UseCookie=False
	 * Иначе HttpClient будет сохранять все куки у себя, а так RestSharp сам ими управляет
	 */

	/// <summary>
	/// Находит RestClient по прокси
	/// </summary>
	/// <param name="proxy">Прокси, через который будут проходить запросы</param>
	/// <param name="uri">Url запроса, нужно только для получения url прокси <see cref="IWebProxy.GetProxy(Uri)"/></param>
	/// <returns>RestClient для работы с запросами</returns>
	public static RestClient GetClient(IWebProxy? proxy, Uri uri)
	{
		var hash = proxy?.GetProxy(uri)?.GetHashCode() ?? uri.GetHashCode();
		RestClient? client = null;
		_clients!.AddOrUpdate(hash, (key) =>
		{
			client = _funcGetClient?.Invoke(proxy);
			return client!;
		},
		(key, oldValue) =>
		{
			client = oldValue;
			return oldValue;
		});
		return client!;
	}
	public static void SetFuncConfigureClient(Func<IWebProxy?, RestClient> getClient)
	{
		_funcGetClient = getClient;
	}

	private static RestClient DefaultConfigureClient(IWebProxy? proxy)
	{
		var client = new RestClient(new RestClientOptions()
		{
			AutomaticDecompression = DecompressionMethods.All,
			FollowRedirects = false,
			UserAgent = null,
			AllowMultipleDefaultParametersWithSameName = true,
			Proxy = proxy,
			// если скрыть, то при Redirect куки сбрасываются
			CookieContainer = null, // из-за этого происходит дублирование кук и если какие-то куки изменились, то будут изменённые и не изменённые
			ConfigureMessageHandler = h =>
			{
				((HttpClientHandler)h).UseCookies = false; // если это не установить, то Set-Cookie не работает
														  // если скрыть, то куки записываются в этот контейнер, в который мы не можем попасть
				((HttpClientHandler)h).CookieContainer = new(); // если это не установить, то куки из Set-Cookie не добавляются в наш CookieContainer
				((HttpClientHandler)h).UseProxy = proxy != null;
				((HttpClientHandler)h).Proxy = proxy;
				return h;
			}
		});
		return client;
	}
}