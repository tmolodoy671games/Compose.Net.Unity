// ReSharper disable ArrangeNamespaceBody

using System;
using Compose.Net;
using Compose.Net.Navigation;
using global::Unity.Scripting.LifecycleManagement;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    [NoAutoStaticsCleanup]
    internal partial class NavigationSharedTransitionSample : ComposeUI
    {
        private sealed record Home : INavKey;

        private sealed record ProductList : INavKey;

        private sealed record Product(string Id, string Title, Color Color) : INavKey;

        private sealed record Settings : INavKey;

        private static readonly Color Background = Color.FromBytes(22, 24, 30);
        private static readonly Color Muted = Color.FromBytes(150, 156, 176);
        private static readonly Color ProductsColor = Color.DodgerBlue;
        private static readonly Color SettingsColor = Color.MediumSeaGreen;

        private static readonly Product[] Products =
        {
            new("compose", "Compose.Net", Color.Coral),
            new("nav3", "Navigation 3", Color.MediumPurple),
            new("uitk", "Unity UI Toolkit", Color.Gold)
        };

        [Composable]
        protected override void Content() => Layout();

        [Composable]
        protected override void Preview() => Layout();

        [Composable]
        private static void Layout()
        {
            var tween = Tween(500);
            var fade = FadeIn(tween).TogetherWith(FadeOut(tween));
            var backStack = RememberNavBackStack<INavKey>(new Home());
            SharedTransitionLayout(
                modifier: Modifier.FillMaxSize().Background(Background),
                content: shared =>
                {
                    NavDisplay(
                        backStack,
                        onBack: () => backStack.RemoveLastOrNull(),
                        modifier: Modifier.FillMaxSize(),
                        contentAlignment: Alignment.Center,
                        transitionSpec: _ => fade,
                        popTransitionSpec: _ => fade,
                        // Shared element reclaim + retain currently leaves nested text transforms
                        // stuck; match SharedTransitionLayoutSample and dispose off-stack entries.
                        retainEntries: false,
                        entryProvider: EntryProvider<INavKey>(scope =>
                        {
                            scope.Entry<Home>(() => HomeScreen(shared, backStack));
                            scope.Entry<ProductList>(() => ProductListScreen(shared, backStack));
                            scope.Entry<Product>(product => ProductScreen(shared, backStack, product));
                            scope.Entry<Settings>(() => SettingsScreen(shared, backStack));
                        })
                    );
                }
            );
        }

        [Composable]
        private static void HomeScreen(SharedTransitionScope shared, NavBackStack<INavKey> backStack)
        {
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Center,
                modifier: Modifier.FillMaxSize().Padding(24.Dp()),
                content: () =>
                {
                    Text("Home", color: Color.White, fontSize: 32.Sp());
                    Text(
                        "Кнопка превращается в следующий экран, текст — в заголовок.",
                        color: Muted,
                        fontSize: 16.Sp(),
                        modifier: Modifier.Padding(top: 8.Dp(), bottom: 16.Dp())
                    );
                    SharedNavButton(
                        shared,
                        containerKey: ScreenKey(nameof(ProductList)),
                        titleKey: TitleKey(nameof(ProductList)),
                        text: "Товары",
                        color: ProductsColor,
                        onClick: () => backStack.Add(new ProductList())
                    );
                    SharedNavButton(
                        shared,
                        containerKey: ScreenKey(nameof(Settings)),
                        titleKey: TitleKey(nameof(Settings)),
                        text: "Настройки",
                        color: SettingsColor,
                        onClick: () => backStack.Add(new Settings())
                    );
                }
            );
        }

        [Composable]
        private static void ProductListScreen(SharedTransitionScope shared, NavBackStack<INavKey> backStack)
        {
            SharedScreen(
                shared,
                containerKey: ScreenKey(nameof(ProductList)),
                titleKey: TitleKey(nameof(ProductList)),
                title: "Товары",
                color: ProductsColor,
                backStack: backStack,
                content: () =>
                {
                    foreach (var product in Products)
                    {
                        SharedNavButton(
                            shared,
                            containerKey: ScreenKey(product),
                            titleKey: TitleKey(product),
                            text: product.Title,
                            color: product.Color,
                            onClick: () => backStack.Add(product)
                        );
                    }
                }
            );
        }

        [Composable]
        private static void ProductScreen(
            SharedTransitionScope shared,
            NavBackStack<INavKey> backStack,
            Product product
        )
        {
            SharedScreen(
                shared,
                containerKey: ScreenKey(product),
                titleKey: TitleKey(product),
                title: product.Title,
                color: product.Color,
                backStack: backStack,
                content: () =>
                {
                    Text("id: " + product.Id, color: Color.White with { Alpha = 0.75f }, fontSize: 18.Sp());
                }
            );
        }

        [Composable]
        private static void SettingsScreen(SharedTransitionScope shared, NavBackStack<INavKey> backStack)
        {
            var dark = Remember(() => MutableStateOf(true));
            SharedScreen(
                shared,
                containerKey: ScreenKey(nameof(Settings)),
                titleKey: TitleKey(nameof(Settings)),
                title: "Настройки",
                color: SettingsColor,
                backStack: backStack,
                content: () =>
                {
                    Text(
                        dark.Value ? "Тема: тёмная" : "Тема: светлая",
                        color: Color.White,
                        fontSize: 18.Sp(),
                        modifier: Modifier
                            .Padding(top: 12.Dp())
                            .Clickable(() => dark.Value = !dark.Value)
                            .Background(Color.Black with { Alpha = 0.25f }, RoundedCornerShape(12.Dp()))
                            .Padding(horizontal: 20.Dp(), vertical: 12.Dp())
                    );
                }
            );
        }

        [Composable]
        private static void SharedScreen(
            SharedTransitionScope shared,
            object containerKey,
            object titleKey,
            string title,
            Color color,
            NavBackStack<INavKey> backStack,
            [Composable] Action content
        )
        {
            var animated = LocalAnimatedVisibilityScope.Current;
            var tween = Tween(500);
            Column(
                horizontalAlignment: Alignment.CenterHorizontally,
                verticalArrangement: Arrangement.Top,
                modifier: Modifier
                    .FillMaxSize()
                    .SharedBounds(
                        shared.RememberSharedContentState(containerKey),
                        animated,
                        enter: IEnterTransition.None,
                        exit: IExitTransition.None,
                        boundsTransform: (_, _) => tween
                    )
                    .Background(color)
                    .Padding(24.Dp()),
                content: () =>
                {
                    Row(
                        verticalAlignment: Alignment.CenterVertically,
                        modifier: Modifier.Padding(bottom: 16.Dp()),
                        content: () =>
                        {
                            Text(
                                "Назад",
                                color: Color.White,
                                fontSize: 16.Sp(),
                                modifier: Modifier
                                    .Padding(right: 12.Dp())
                                    .Clickable(() => backStack.RemoveLastOrNull())
                                    .Background(Color.Black with { Alpha = 0.25f }, RoundedCornerShape(10.Dp()))
                                    .Padding(horizontal: 16.Dp(), vertical: 8.Dp())
                            );

                            Text(
                                title,
                                color: Color.White,
                                fontSize: 64.Sp(),
                                // sharedBounds: text size differs between button and title.
                                modifier: Modifier.SharedBounds(
                                    shared.RememberSharedContentState(titleKey),
                                    animated,
                                    enter: IEnterTransition.None,
                                    exit: IExitTransition.None,
                                    boundsTransform: (_, _) => tween
                                )
                            );
                        }
                    );
                    content();
                }
            );
        }

        [Composable]
        private static void SharedNavButton(
            SharedTransitionScope shared,
            object containerKey,
            object titleKey,
            string text,
            Color color,
            Action onClick
        )
        {
            var animated = LocalAnimatedVisibilityScope.Current;
            var tween = Tween(500);
            Box(
                contentAlignment: Alignment.Center,
                modifier: Modifier
                    .Padding(top: 12.Dp())
                    .Clickable(onClick, indication: null, interactionSource: null)
                    .SharedBounds(
                        shared.RememberSharedContentState(containerKey),
                        animated,
                        enter: IEnterTransition.None,
                        exit: IExitTransition.None,
                        boundsTransform: (_, _) => tween
                    )
                    .Background(color, RoundedCornerShape(16.Dp()))
                    .Padding(horizontal: 24.Dp(), vertical: 16.Dp()),
                content: () => Text(
                    text,
                    color: Color.White,
                    fontSize: 32.Sp(),
                    modifier: Modifier.SharedBounds(
                        shared.RememberSharedContentState(titleKey),
                        animated,
                        enter: IEnterTransition.None,
                        exit: IExitTransition.None,
                        boundsTransform: (_, _) => tween
                    )
                )
            );
        }

        private static string ScreenKey(string route) => "screen:" + route;

        private static string ScreenKey(Product product) => "screen:product:" + product.Id;

        private static string TitleKey(string route) => "title:" + route;

        private static string TitleKey(Product product) => "title:product:" + product.Id;
    }
}