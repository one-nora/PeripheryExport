using AssetRipper.GUI.Web.Paths;

namespace AssetRipper.GUI.Web.Pages;

public sealed class IndexPage : DefaultPage
{
	public static IndexPage Instance { get; } = new();

	public override string? GetTitle() => GameFileLoader.Premium ? Localization.AssetRipperPremium : Localization.AssetRipperFree;

	private static void WriteGetLink(TextWriter writer, string url, string name, string? @class = null)
	{
		using (new Form(writer).WithAction(url).WithMethod("get").End())
		{
			new Input(writer).WithType("submit").WithClass(@class).WithValue(name.ToHtml()).Close();
		}
	}

	private static void WritePostLink(TextWriter writer, string url, string name, string? @class = null)
	{
		using (new Form(writer).WithAction(url).WithMethod("post").End())
		{
			new Input(writer).WithType("submit").WithClass(@class).WithValue(name.ToHtml()).Close();
		}
	}
	
	public override void WriteInnerContent(TextWriter writer)
	{
		using (new Div(writer).WithClass("text-center container mt-5").End())
		{
			new H1(writer).WithClass("display-4 mb-4").Close("Periphery Export");
			
			if (GameFileLoader.IsLoaded)
			{
				using (new Div(writer).WithClass("d-flex justify-content-center").End())
				{
					WriteGetLink(writer, "/Commands", "Generate RIFT project", "btn btn-success m-1");
					WritePostLink(writer, "/Reset", Localization.MenuFileReset, "btn btn-danger m-1");
				}
			}
			else
			{
				using (new Form(writer).WithClass("text-left mt-2").WithAction("/LoadFolder").WithMethod("post").End())
				{
					new Button(writer).WithClass("btn btn-primary m-1").WithType("submit").Close("Open White Knuckle folder");
				}
			}
			
			new P(writer).WithClass("mt-4").Close("Donate for Asset Ripper, the original project:");
			using (new Div(writer).WithClass("d-flex justify-content-center mt-3").End())
			{
				new A(writer).WithClass("btn btn-danger m-1").WithNewTabAttributes().WithHref("https://patreon.com/ds5678").Close("Patreon");
				new A(writer).WithClass("btn btn-danger m-1").WithNewTabAttributes().WithHref("https://paypal.me/ds5678").Close("Paypal");
				new A(writer).WithClass("btn btn-danger m-1").WithNewTabAttributes().WithHref("https://github.com/sponsors/ds5678").Close("GitHub Sponsors");
			}
		}
	}
}
