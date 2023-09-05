# GrassGPUInstances
![Grass1.jpg](..%2FPictures%2FScreenShot%2FGrass1.jpg)
## 1.使用方法
    使用时在管线的Renderer中添加一个RenderFeature(Grass Render Feature)
    然后在场景中添加一个Panel命名为GrassTerrain并将材质替换成Terrain.Mat (目录是Assets\GrassGPUInstances\Materials\Terrain.mat)
    在Panel里面添加脚本GrassTerrain.cs,GroundGenerator.cs,LightingManager.cs
## 2.脚本设置
    GroundGenerator.cs:使用来生成地形mesh,Size参数可以设置地形的大小.
    GrassTerrain.cs:用来设置草的参数,包括草的数量,草的大小,草的颜色等.使用时将GrassMaterial拖入Material里.
    LightingManager.cs:用来设置光照的参数,包括光照的方向,光照的颜色,强度.环境光颜色等
## 3.生长效果
    在GrassTerrain.cs里分别有两个参数控制草生长动画;
    Grow:控制草生长的进度,0-1之间的值,0表示草还没有生长,1表示草已经生长完毕.
    GrowDir:控制草生长的方向.
