// ReSharper disable ArrangeNamespaceBody

using System;
using Compose.Net;
using Compose.Net.Navigation;
using static Compose.Net.Navigation.NavigationFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    internal partial class NavigationSample : ComposeUI
    {
        private sealed record Home : INavKey;

        private sealed record ProductList : INavKey;

        private sealed record Product(string Id, string Title) : INavKey;

        private sealed record Settings : INavKey;

        private static readonly Color Background = Color.FromBytes(22, 24, 30);
        private static readonly Color Surface = Color.FromBytes(34, 37, 46);
        private static readonly Color Muted = Color.FromBytes(150, 156, 176);

        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            var backStack = RememberNavBackStack<INavKey>(new Home());
            NavDisplay(
                backStack,
                onBack: () => backStack.RemoveLastOrNull(),
                modifier: Modifier.FillMaxSize().Background(Background),
                contentAlignment: Alignment.Center,
                entryProvider: EntryProvider<INavKey>(scope =>
                {
                    scope.Entry<Home>(() => HomeScreen(backStack));
                    scope.Entry<ProductList>(() => ProductListScreen(backStack));
                    scope.Entry<Product>(product => ProductScreen(backStack, product));
                    scope.Entry<Settings>(() => SettingsScreen(backStack));
                })
            );
        }

        [Composable]
        private static void HomeScreen(NavBackStack<INavKey> backStack)
        {
            var visits = Remember(() => MutableStateOf(0));
            Screen(
                title: "Home",
                backStack: backStack,
                content: () =>
                {
                    Text(
                        "Счётчик живёт в Remember этого экрана. Уйдите и вернитесь — значение должно сохраниться.",
                        color: Muted,
                        fontSize: 16.Sp()
                    );
                    NavButton("Визитов: " + visits.Value, () => visits.Value++);
                    NavButton("Товары", () => backStack.Add(new ProductList()));
                    NavButton("Настройки", () => backStack.Add(new Settings()));
                }
            );
        }

        [Composable]
        private static void ProductListScreen(NavBackStack<INavKey> backStack)
        {
            Screen(
                title: "Товары",
                backStack: backStack,
                content: () =>
                {
                    NavButton("Compose.Net", () => backStack.Add(new Product("compose", "Compose.Net")));
                    NavButton("Navigation 3", () => backStack.Add(new Product("nav3", "Navigation 3")));
                    NavButton("Unity UI Toolkit", () => backStack.Add(new Product("uitk", "Unity UI Toolkit")));
                }
            );
        }

        [Composable]
        private static void ProductScreen(NavBackStack<INavKey> backStack, Product product)
        {
            Screen(
                title: product.Title,
                backStack: backStack,
                content: () =>
                {
                    Text("id: " + product.Id, color: Muted, fontSize: 18.Sp());
                    NavButton("Ещё один экран того же типа", () =>
                        backStack.Add(new Product(product.Id + "+", product.Title + " +"))
                    );
                }
            );
        }

        [Composable]
        private static void SettingsScreen(NavBackStack<INavKey> backStack)
        {
            var dark = Remember(() => MutableStateOf(true));
            Screen(
                title: "Настройки",
                backStack: backStack,
                content: () =>
                {
                    NavButton(
                        dark.Value ? "Тема: тёмная" : "Тема: светлая",
                        () => dark.Value = !dark.Value
                    );
                }
            );
        }

        [Composable]
        private static void Screen(
            string title,
            NavBackStack<INavKey> backStack,
            [Composable] Action content
        )
        {
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Center,
                modifier: Modifier.FillMaxSize().Padding(24.Dp()),
                content: () =>
                {
                    Row(
                        verticalAlignment: Alignment.CenterVertically,
                        modifier: Modifier.Padding(bottom: 16.Dp()),
                        content: () =>
                        {
                            if (backStack.Size > 1)
                            {
                                Text(
                                    "Назад",
                                    color: Color.White,
                                    fontSize: 16.Sp(),
                                    modifier: Modifier
                                        .Padding(right: 12.Dp())
                                        .Background(Surface, RoundedCornerShape(10.Dp()))
                                        .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                                        .Clickable(() => backStack.RemoveLastOrNull())
                                );
                            }

                            Text(title, color: Color.White, fontSize: 32.Sp());
                        }
                    );
                    Text("Стек: " + backStack.Size, color: Muted, fontSize: 14.Sp());
                    content();
                }
            );
        }

        [Composable]
        private static void NavButton(string text, Action onClick)
        {
            Text(
                text,
                color: Color.White,
                fontSize: 18.Sp(),
                modifier: Modifier
                    .Padding(top: 12.Dp())
                    .Background(Surface, RoundedCornerShape(12.Dp()))
                    .Padding(horizontal: 20.Dp(), vertical: 12.Dp())
                    .Clickable(onClick)
            );
        }
    }
}
