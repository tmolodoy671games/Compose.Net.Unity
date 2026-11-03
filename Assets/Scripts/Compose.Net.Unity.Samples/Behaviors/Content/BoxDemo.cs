using System;
using System.Collections.Generic;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using UnityEngine.UIElements;

namespace Compose.Net.Unity.Samples.Behaviors.Content;

/// <summary>Thirty-two interactive Box examples with live measurement and native picking.</summary>
public partial class BoxDemo : ComposeUI
{
    [Composable] protected override void Content() => Screen(Remember(() => new BoxDemoState()));
    [Composable] protected override void Preview() => Screen(Remember(() => new BoxDemoState()));
    private static readonly Color Ink = new(.04f,.06f,.10f);
    private static readonly Color Surface = new(.09f,.13f,.20f);
    private static readonly Color Stage = new(.025f,.04f,.065f);
    private static readonly Color Blue = new(.16f,.55f,.95f,.85f);
    private static readonly Color Gold = new(1f,.66f,.20f,.85f);
    public static readonly Alignment[] Alignments = { Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd,
        Alignment.CenterStart, Alignment.Center, Alignment.CenterEnd, Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd };

    [Composable] public static void Screen(BoxDemoState state)
    {
        ReusableComposeNode(() => new GalleryNode(), modifier: Modifier.FillMaxSize().Background(Ink).TestTag("box-demo"), content: () =>
        {
            Text("Box",fontSize:36.Sp(),color:Color.White,modifier:Modifier.Height(50.Dp()));
            Text("32 live examples · change each case · click overlapping layers · scroll to explore",fontSize:14.Sp(),color:Color.LightGray,modifier:Modifier.Height(44.Dp()));
            Text("Blue and gold are children. The lighter surface is the measured Box. Start / End currently follow LTR.",fontSize:13.Sp(),color:Color.LightGray,modifier:Modifier.Height(60.Dp()));
            ReusableComposeNode(() => new UnityReusableComposeNode(new VisualElement
            {
                pickingMode = UnityEngine.UIElements.PickingMode.Ignore,
                style =
                {
                    flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart, alignContent = Align.FlexStart,
                    alignSelf = Align.Stretch, flexShrink = 0,
                    rowGap = 12, columnGap = 12,
                }
            }), content: () =>
            {
                foreach(var item in state.Cases) Key(item.Id, () => Card(item));
            });
        });
    }

    [Composable, RowScope] public static void Card(BoxDemoCase item)
    {
        var step = item.Step.Value;
        var measured = item.Measured.Value;
        var click = item.LastClick.Value;
        Column(modifier: Modifier.Width(320.Dp()).Weight(1).Height(310.Dp()).Background(Surface,RoundedCornerShape(10.Dp())).Padding(12.Dp()).TestTag("box."+item.Id+".card"),content:()=>
        {
            Text(item.Title,fontSize:17.Sp(),color:Color.White);
            Text(item.Description,fontSize:12.Sp(),color:Color.LightGray,modifier:Modifier.Height(48.Dp()));
            Box(modifier:Modifier.Size(280.Dp(),160.Dp()).Background(Stage).TestTag("box."+item.Id+".stage"),content:()=>Example(item,step));
            Text($"Box {measured.Width:0} × {measured.Height:0}"+(click.Length==0?"":" · clicked "+click),fontSize:12.Sp(),color:Color.LightGray,modifier:Modifier.TestTag("box."+item.Id+".measure"));
            Row(content:()=>
            {
                Control(item,"change","Change",()=>item.Step.Value++);
                Control(item,"reset","Reset",()=>{item.Step.Value=0;item.LastClick.Value="";});
                Text("State "+step,fontSize:12.Sp(),color:Color.LightGray);
            });
        });
    }
    [Composable, BoxScope] private static void Example(BoxDemoCase item,int step)
    {
        var alternate=step%2!=0;
        var modifier=Modifier.Background(new Color(.20f,.27f,.37f)).TestTag("box."+item.Id+".box").OnSizeChanged(size=>item.Measured.Value=size);
        var alignment=Alignment.TopStart;
        var propagate=false;
        if(item.AlignmentIndex>=0)
        {
            alignment=Alignments[(item.AlignmentIndex+(alternate?4:0))%9];
            modifier=modifier.Size(260.Dp(),140.Dp());
        }
        else switch(item.Id)
        {
            case "empty":modifier=modifier.Size((alternate?160:80).Dp(),(alternate?100:50).Dp()).Clickable(()=>item.LastClick.Value="empty Box");break;
            case "empty-min":modifier=modifier.SizeIn(minWidth:(alternate?150:90).Dp(),minHeight:60.Dp());break;
            case "minimum":modifier=modifier.SizeIn(minWidth:150.Dp(),minHeight:100.Dp());propagate=alternate;break;
            case "fixed-min":modifier=modifier.Size(220.Dp(),120.Dp()).Padding(10.Dp());propagate=alternate;break;
            case "maximum":modifier=modifier.SizeIn(maxWidth:(alternate?110:220).Dp(),maxHeight:120.Dp());break;
            case "padding":modifier=modifier.Padding((alternate?20:10).Dp());alignment=Alignment.Center;break;
            case "only-match":modifier=modifier.SizeIn(minWidth:(alternate?140:0).Dp(),minHeight:(alternate?90:0).Dp());break;
            case "fill":case "fraction":case "fill-width":case "fill-height":modifier=modifier.SizeIn(maxWidth:240.Dp(),maxHeight:130.Dp());break;
            case "child-align":case "nested":case "order":modifier=modifier.Size(240.Dp(),130.Dp());alignment=Alignment.Center;break;
            case "resize":modifier=modifier.Size((alternate?150:240).Dp(),(alternate?90:130).Dp());alignment=Alignment.BottomEnd;break;
            case "text":modifier=modifier.Width((alternate?130:240).Dp());break;
            case "scroll":modifier=modifier.Size(240.Dp(),130.Dp()).VerticalScroll(RememberScrollState());break;
        }
        if(item.Id is "empty" or "empty-min") Box(modifier);
        else Box(modifier:modifier,contentAlignment:alignment,propagateMinConstraints:propagate,content:()=>Layers(item,step));
    }
    [Composable, BoxScope] private static void Layers(BoxDemoCase item,int step)
    {
        var alternate=step%2!=0;
        if(item.AlignmentIndex>=0)
        { Tile(item,"wide",120,35,Blue);Tile(item,"tall",45,85,Gold);return; }
        switch(item.Id)
        {
            case "largest":Tile(item,"wide",alternate?90:180,35,Blue);Tile(item,"tall",45,alternate?120:80,Gold);break;
            case "minimum":case "fixed-min":Tile(item,"small",60,35,Gold);break;
            case "maximum":Tile(item,"large",400,200,Blue);break;
            case "padding":Tile(item,"wide",140,35,Blue);Tile(item,"tall",45,90,Gold);break;
            case "match":
                Tile(item,"normal",alternate?180:110,alternate?100:60,Blue);
                Box(Modifier.MatchParentSize().Border(BorderStroke(3.Dp(),Gold),RoundedCornerShape(0.Dp())).TestTag("box."+item.Id+".overlay"));break;
            case "only-match":Box(Modifier.Size(400.Dp(),300.Dp()).MatchParentSize().Background(Gold).TestTag("box."+item.Id+".overlay"));break;
            case "fill":Box(Modifier.FillMaxSize().Background(Blue));if(alternate)Tile(item,"small",45,30,Gold,Modifier.Align(Alignment.Center));break;
            case "fraction":Box(Modifier.FillMaxSize(alternate ? .75f : .5f).Background(Gold));break;
            case "fill-width":Box(Modifier.FillMaxWidth(alternate ? .5f : 1).Height(50.Dp()).Background(Blue));break;
            case "fill-height":Box(Modifier.FillMaxHeight(alternate ? .5f : 1).Width(60.Dp()).Background(Gold));break;
            case "child-align":Tile(item,"inherited",120,80,Blue);Tile(item,"override",45,30,Gold,Modifier.Align(alternate?Alignment.TopStart:Alignment.BottomEnd));break;
            case "parent-data":Tile(item,"normal",160,100,Blue);Tile(item,"data",45,30,Gold,alternate?Modifier.MatchParentSize().Align(Alignment.BottomEnd):Modifier.Align(Alignment.BottomEnd).MatchParentSize());break;
            case "toggle-match":Tile(item,"normal",160,100,Blue);Tile(item,"toggle",45,30,Gold,alternate?Modifier.MatchParentSize():Modifier.Align(Alignment.Center));break;
            case "order":
                for(var i=0;i<2;i++){var id=alternate?1-i:i;Key(id,()=>Tile(item,id==0?"blue":"gold",120,80,id==0?Blue:Gold,Modifier.Clickable(()=>item.LastClick.Value=id==0?"blue":"gold")));}break;
            case "offset":Tile(item,"base",140,80,Blue);Tile(item,"offset",60,40,Gold,Modifier.Offset((alternate?90:20).Dp(),25.Dp()));break;
            case "nested":Box(modifier:Modifier.Size(180.Dp(),90.Dp()).Background(Blue).Align(alternate?Alignment.TopStart:Alignment.BottomEnd),contentAlignment:Alignment.BottomEnd,content:()=>Tile(item,"inner",45,30,Gold));break;
            case "resize":Tile(item,"child",60,35,Gold);break;
            case "text":Text("UI Toolkit measures and wraps this text. Change the width and watch Box follow its height.",fontSize:16.Sp(),color:Color.White);break;
            case "scroll":Tile(item,"scroll-child",200,alternate?450:300,Blue);Tile(item,"top",90,50,Gold);break;
            case "dynamic":
                for(var i=0;i<step%4;i++){var n=i;Key(n,()=>Tile(item,"item-"+n,80+n*50,40+n*30,n%2==0?Blue:Gold));}break;
        }
    }
    [Composable] private static void Tile(BoxDemoCase item,string id,int width,int height,Color color,IModifier? extra=null)
        => Box(Modifier.Size(width.Dp(),height.Dp()).Background(color).Then(extra).TestTag("box."+item.Id+"."+id));
    [Composable] private static void Control(BoxDemoCase item,string id,string title,Action action)
        => ReusableComposeNode(() => new DemoButton(), initializer: node => ((DemoButton)node).Update(title, action),
            modifier: Modifier.TestTag("box."+item.Id+"."+id));

    private sealed class GalleryNode : UnityReusableComposeNode
    {
        public GalleryNode() : base(new ScrollView(ScrollViewMode.Vertical))
        {
            var scroll=(ScrollView)VisualElement;
            scroll.pickingMode=UnityEngine.UIElements.PickingMode.Position;
            scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility=ScrollerVisibility.Hidden;
            scroll.nestedInteractionKind=ScrollView.NestedInteractionKind.ForwardScrolling;
            var content=scroll.contentContainer.style;
            content.flexGrow=0;content.alignItems=Align.Stretch;
            content.paddingLeft=content.paddingRight=content.paddingTop=content.paddingBottom=20;
        }
    }

    // Standard UI Toolkit Button supplies focus, keyboard activation, picking and pressed feedback.
    private sealed class DemoButton : UnityReusableComposeNode
    {
        private Action? _action;
        public DemoButton() : base(new Button())
        {
            var button=(Button)VisualElement;
            button.pickingMode=UnityEngine.UIElements.PickingMode.Position;
            button.clicked+=()=>_action?.Invoke();
            button.style.height=28;button.style.minWidth=58;button.style.marginRight=6;
            button.style.marginTop=2;button.style.marginBottom=2;
        }
        internal void Update(string title,Action action)
        { ((Button)VisualElement).text=title;_action=action; }
    }

}

public sealed class BoxDemoCase
{
    public readonly string Id,Title,Description;
    public readonly int AlignmentIndex;
    public readonly IMutableState<int> Step=MutableStateOf(0);
    public readonly IMutableState<Size> Measured=MutableStateOf(new Size(0,0));
    public readonly IMutableState<string> LastClick=MutableStateOf("");
    public BoxDemoCase(string id,string title,string description,int alignmentIndex=-1)
    {Id=id;Title=title;Description=description;AlignmentIndex=alignmentIndex;}
}
public sealed class BoxDemoState
{
    public readonly BoxDemoCase[] Cases;
    public BoxDemoState()
    {
        var cases=new List<BoxDemoCase>();
        var names=new[]{"TopStart","TopCenter","TopEnd","CenterStart","Center","CenterEnd","BottomStart","BottomCenter","BottomEnd"};
        for(var i=0;i<names.Length;i++)cases.Add(new BoxDemoCase("align-"+i,names[i],"contentAlignment applies to both children. Change cycles the default alignment.",i));
        cases.AddRange(new[]{
            new BoxDemoCase("largest","Size to content","Width and height come from different children. Change their sizes."),
            new BoxDemoCase("empty","Empty Box","The modifier-only overload can draw, resize and receive clicks."),
            new BoxDemoCase("empty-min","Empty + minimum","An empty Box uses the incoming minimum dimensions."),
            new BoxDemoCase("minimum","propagateMinConstraints","Change toggles minimum propagation: only Box, then Box and child."),
            new BoxDemoCase("fixed-min","Fixed constraints + padding","With propagation enabled, the child fills the fixed inner size."),
            new BoxDemoCase("maximum","Maximum constraints","A 400 × 200 child is constrained before placement. Change narrows the maximum."),
            new BoxDemoCase("padding","Padding and center","Internal padding surrounds the whole stack. Change doubles it."),
            new BoxDemoCase("match","MatchParentSize","The gold outline follows the blue child without affecting Box size."),
            new BoxDemoCase("only-match","Only MatchParentSize","The 400 × 300 child contributes no size. Change adds a minimum."),
            new BoxDemoCase("fill","FillMaxSize","A fill child determines Box size using the incoming maximum."),
            new BoxDemoCase("fraction","Fractional fill","Change switches from 50% to 75% of the available constraints."),
            new BoxDemoCase("fill-width","FillMaxWidth","Width fills; height stays 50. Change uses half the available width."),
            new BoxDemoCase("fill-height","FillMaxHeight","Height fills; width stays 60. Change uses half the available height."),
            new BoxDemoCase("child-align","Child Align","Gold overrides the centered default. Change moves only gold."),
            new BoxDemoCase("parent-data","Parent-data order","Align then MatchParentSize: Align wins. Change reverses the chain."),
            new BoxDemoCase("toggle-match","Add / remove MatchParentSize","Change replaces a centered 45 × 30 child with a matching overlay."),
            new BoxDemoCase("order","Composition and picking","Click the overlap. Change reverses keyed children and the top click target."),
            new BoxDemoCase("offset","Offset preserves measurement","Gold moves without changing Box size or the blue child's position."),
            new BoxDemoCase("nested","Nested alignment","Outer and inner Boxes align independently. Change moves the inner Box."),
            new BoxDemoCase("resize","Live parent resize","The bottom-end child follows the parent as it changes size."),
            new BoxDemoCase("text","Intrinsic text measurement","Change narrows the text. Native wrapping changes the measured Box height."),
            new BoxDemoCase("scroll","Unbounded scroll axis","Children overlap inside a native ScrollView. Change increases the tallest child."),
            new BoxDemoCase("dynamic","Add / remove children","Change cycles through zero, one, two and three keyed children."),
        });
        Cases=cases.ToArray();
    }
}
