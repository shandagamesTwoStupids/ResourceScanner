using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Text;

public class ResourceScanner : EditorWindow {

    //菜单入口
    [MenuItem("UI/ResourceScanner")]
    static void Init() {
		ResourceScanner window = (ResourceScanner)EditorWindow.GetWindow(typeof(ResourceScanner));
        window.Show();
    }

	#region 数据准备
	//资源类型
	const string MATERIAL = "material";
	const string TEXTURE2D = "texture2D";
	const string AUDIO_CLIP = "audioClip";
	const string SPRITE = "sprite";
	const string FONT = "font";
	const string MESH = "mesh";
	const string ANIMATION_CLIP = "animationClip";
	const string RUNTIME_ANIMATOR_CONTROLLER = "runtimeAnimatorController";
	const string AVATOR = "avator";
	const string PREFAB = "prefab";

	//查找资源文件夹下带后缀名的资源
	private static readonly string[] ResourceExts = {".prefab",".png",".jpg",".dds",".gif",".psd",".controller",".shader",".anim",".mat",".wav",".mp3"};

	//保存资源相关信息
	public class ResourceHolder {
		public Object ResourceObject;
		public string ResourcePath;
		public string ResourceName;
		public string ResourceType;
		public int ResourceUseCount;
		public string UseSceneName;
        public string ResourceMd5;
	}
	//保存资源文件下资源信息
	class DirsResourceHolder{
		public string name;
		public string path;
		public string postfix;
	}
	//保存未激活的gameobject信息
	public class InActiveHolder {
		public GameObject inActiveObject;
		public string path;
	}

    //保存的资源列表
    private Dictionary<string, ResourceHolder> mResourceDict = new Dictionary<string, ResourceHolder>();
    private List<ResourceHolder> mResourceList = new List<ResourceHolder>();
	private List<DirsResourceHolder> mDirsResourceList = new List<DirsResourceHolder>();
	private List<DirsResourceHolder> mUnUsedResourceList = new List<DirsResourceHolder> ();
	private List<InActiveHolder> mInActiveList = new List<InActiveHolder>();

    //场景的默认文件路径
    private string mSceneFolderPath = "Assets/03.script/";
    //保存需要扫描场景名字的列表文件
    private string mSceneNameListFilePath = "sceneList.csv";
    //用于保存的文件地址
    private string mSaveFolderPath = "D:/workspace/test/";
    //用于保存记录的文件
    //private string mSaveFileName = "default.csv";
    //显示宽度（每列）
    const float WIDTH = 100f;
    //用于保存滚动内容位置
    //private Vector2 mScrollPos = new Vector2(0, 0);
    //用于显示资源排序方向
    //private string mTypeSortStr = "资源类型";
    //private string mUseSortStr = "资源使用次数";
    //private const string UP = "↑";
    //private const string DOWN = "↓";
    //private bool mTypeSortOrder = true;
    //private bool mUseSortOrder = true;
    #endregion

    //绘制
    void OnGUI() {
        //使用说明
        if (GUILayout.Button("使用说明")) {
            //EditorWindow.GetWindow(typeof(IllustrationWindow)).Show();
			BuildUnUsedResourceList();
        }

        //设置保存文件夹位置
        GUILayout.BeginHorizontal();
        GUILayout.Label("插件工作文件夹路径");
        GUILayout.Label(mSaveFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(WIDTH))) {
            //选择文件夹位置
            mSaveFolderPath = EditorUtility.OpenFolderPanel("请选择保存文件夹", mSaveFolderPath, 
				mSaveFolderPath.Substring(0, mSaveFolderPath.Length - 1));
            mSaveFolderPath += "/";
        }
        GUILayout.EndHorizontal();

        //设置场景文件夹位置
        GUILayout.BeginHorizontal();
        GUILayout.Label("项目场景文件夹路径");
        GUILayout.Label(mSceneFolderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(WIDTH))) {
            //选择文件夹位置
            Debug.Log(mSceneFolderPath);
            string path = EditorUtility.OpenFolderPanel("请选择场景文件夹", mSceneFolderPath, 
				mSceneFolderPath.Substring(0, mSceneFolderPath.Length - 1));
            if (path.Contains("Assets")) {
                int index = path.IndexOf("Assets");
                mSceneFolderPath = path.Substring(index) + "/";
            } else {
                Debug.Log("请确认选择的目录包含Assets（资源目录）");
            }
            Debug.Log(mSceneFolderPath);
        }
        GUILayout.EndHorizontal();

        //设置场景文件夹位置
        GUILayout.BeginHorizontal();
        GUILayout.Label("场景列表文件路径(请将场景列表放入其中)");
        GUILayout.Label(mSaveFolderPath + mSceneNameListFilePath);
        GUILayout.EndHorizontal();

        //扫描资源并保存（按场景名）
        if (GUILayout.Button("扫描当前场景资源并保存")) {
            ClearResourceList();
            Scene nowScene = EditorSceneManager.GetActiveScene();
            BuildResourceList(nowScene);
            if (SaveResToFile(mSaveFolderPath, nowScene.name)) {
                Debug.Log("保存成功");
            }
        }

        //对每个提供名字的场景依次扫描并按场景名保存为文件
        if (GUILayout.Button("扫描列表(sceneList)中所有场景（分别保存）")) {
            //记录当前场景路径
            string nowScenePath = EditorSceneManager.GetActiveScene().path;
            string[] nameOfScenes = GetNameOfFilesToScan(mSaveFolderPath + mSceneNameListFilePath);
            float progress = 0f;
            float progressStep = 1.0f / nameOfScenes.Length;
            for (int i = 0; i < nameOfScenes.Length; i++) {
                EditorUtility.DisplayProgressBar("扫描中", nameOfScenes[i], progress += progressStep);
                Scene scene;
                try {
                    scene = EditorSceneManager.OpenScene(mSceneFolderPath + nameOfScenes[i] + ".unity", OpenSceneMode.Single);
                } catch (System.Exception e) {
                    Debug.Log(e.Message);
                    continue;
                }
                if (!scene.IsValid()) {
                    Debug.Log("加载失败，场景名：" + nameOfScenes[i]);
                    continue;
                }
                BuildResourceList(scene);
                if (SaveResToFile(mSaveFolderPath, scene.name)) {
                    Debug.Log("保存成功");
                }
                ClearResourceList();
            }
            EditorSceneManager.OpenScene(nowScenePath, OpenSceneMode.Single);
            EditorUtility.ClearProgressBar();
        }

        //扫描当前场景中的未激活对象并保存
        if (GUILayout.Button("扫描当前场景中未激活对象并保存")) {
            ClearResourceList();
            Scene nowScene = EditorSceneManager.GetActiveScene();
            BuildInActiveResourceList(nowScene);
            if (SaveObjToFile(mSaveFolderPath, nowScene.name)) {
                Debug.Log("保存成功");
            }
        }

        //扫描列表中的场景的未激活对象并保存
        if (GUILayout.Button("扫描列表中所有场景的资源和未激活对象并保存")) {
            //记录当前场景路径
            string nowScenePath = EditorSceneManager.GetActiveScene().path;
            string[] nameOfScenes = GetNameOfFilesToScan(mSaveFolderPath + mSceneNameListFilePath);
            float progress = 0f;
            float progressStep = 1.0f / nameOfScenes.Length;
            for (int i = 0; i < nameOfScenes.Length; i++) {
                EditorUtility.DisplayProgressBar("扫描中", nameOfScenes[i], progress += progressStep);
                Scene scene = EditorSceneManager.OpenScene(mSceneFolderPath + nameOfScenes[i] + ".unity", OpenSceneMode.Single);
                if (!scene.IsValid()) {
                    Debug.Log("加载失败，场景名：" + nameOfScenes[i]);
                    continue;
                }
                Debug.Log(scene.name);
                BuildInActiveResourceList(scene);
                if (SaveObjToFile(mSaveFolderPath, scene.name)) {
                    Debug.Log("保存成功");
                }
                ClearResourceList();
            }
            EditorSceneManager.OpenScene(nowScenePath, OpenSceneMode.Single);
            EditorUtility.ClearProgressBar();
        }
        #region 暂时没有需求就不考虑了
        //扫描资源功能按键
        //GUILayout.BeginHorizontal();
        //bool scanBtn = GUILayout.Button("扫描当前场景资源");
        //bool scanSaveBtn = GUILayout.Button("扫描当前场景资源并保存");
        //GUILayout.EndHorizontal();
        //GUILayout.BeginHorizontal();
        //bool scanListBtn = GUILayout.Button("扫描列表(senceList)中所有场景（整合统计）");
        //bool scanListSaveBtn = GUILayout.Button("扫描列表(sceneList)中所有场景（分别保存）");
        //GUILayout.EndHorizontal();
        //核心功能，扫描当前场景，统计资源使用情况
        //if (scanBtn) {
        //    Scene nowScene = EditorSceneManager.GetActiveScene();
        //    BuildResourceList(nowScene);
        //}

        //扫描提供名字的场景中的所有的资源引用
        //if (scanListBtn) {
        //    string[] nameOfScenes = GetNameOfFilesToScan(mSaveFolderPath + mSceneNameListFilePath);
        //    float progress = 0f;
        //    float progressStep = 1.0f / nameOfScenes.Length;
        //    for (int i = 0; i < nameOfScenes.Length; i++) {
        //        EditorUtility.DisplayProgressBar("扫描中", nameOfScenes[i], progress += progressStep);
        //        Scene scene = EditorSceneManager.GetSceneByPath(mSceneFolderPath + nameOfScenes[i] + ".unity");
        //        BuildResourceList(scene);
        //    }
        //    EditorUtility.ClearProgressBar();
        //}

        ////保存到文件
        //if (GUILayout.Button("保存到文件(default.csv)")) {
        //    if (SaveResToFile(mSaveFolderPath + mSaveFileName)) {
        //        Debug.Log("保存成功");
        //    }
        //}

        ////从文件中读取
        //if (GUILayout.Button("从文件中加载(default.csv)")) {
        //    if (LoadFromFile(mSaveFolderPath + mSaveFileName)) {
        //        Debug.Log("加载成功");
        //    }
        //}

        //展示扫描资源使用情况结果
        //if (mResourceList.Count > 0 && mResourceList.Count < 50) {
        //    GUILayout.Label("扫描结果（最多显示50条）");
        //    //表头
        //    GUILayout.BeginHorizontal();
        //    GUILayout.Label("资源名称", GUILayout.Width(WIDTH));
        //    if (GUILayout.Button(mTypeSortStr, GUILayout.Width(WIDTH))) {
        //        SortWithType(mTypeSortOrder);
        //        mTypeSortStr = "资源类型" + (mTypeSortOrder ? UP : DOWN);
        //        mUseSortStr = "资源使用次数";
        //        mTypeSortOrder = !mTypeSortOrder;
        //    }
        //    if (GUILayout.Button(mUseSortStr, GUILayout.Width(WIDTH))) {
        //        SortWithUseCount(mUseSortOrder);
        //        mUseSortStr = "资源使用次数" + (mUseSortOrder ? UP : DOWN);
        //        mTypeSortStr = "资源类型";
        //        mUseSortOrder = !mUseSortOrder;
        //    }
        //    GUILayout.EndHorizontal();
        //    mScrollPos = GUILayout.BeginScrollView(mScrollPos, GUILayout.Width(WIDTH * 3 + 30));
        //    //列表显示内容
        //    for (int i = 0; i < mResourceList.Count; i++) {
        //        GUILayout.BeginHorizontal();
        //        GUILayout.Label(mResourceList[i].ResourceName, GUILayout.Width(WIDTH));
        //        GUILayout.Label(mResourceList[i].ResourceType, GUILayout.Width(WIDTH));
        //        GUILayout.Label(mResourceList[i].ResourceUseCount.ToString(), GUILayout.Width(WIDTH));
        //        GUILayout.EndHorizontal();
        //    }
        //    GUILayout.EndScrollView();

        //    //提供清理方法
        //    if (GUILayout.Button("清空扫描记录")) {
        //        ClearResourceList();
        //    }
        //}

        //扫描资源功能按键
        //GUILayout.BeginHorizontal();
        //bool scanObjBtn = GUILayout.Button("扫描当前场景中未激活对象");
        //bool scanListObjSaveBtn = GUILayout.Button("扫描列表中所有场景的资源和未激活对象并保存");
        //GUILayout.EndHorizontal();
        //扫描当前场景，统计未激活的GameObject
        //if (scanObjBtn) {
        //    Scene nowScene = EditorSceneManager.GetActiveScene();
        //    ClearResourceList();
        //    BuildInActiveResourceList(nowScene);
        //}


        //展示扫描未激活对象结果
        //if (mInActiveList.Count > 0 && mInActiveList.Count < 50) {
        //	GUILayout.Label("扫描结果（最多显示50条）");
        //	GUILayout.BeginHorizontal();
        //	GUILayout.Label("对象名称", GUILayout.Width(WIDTH));
        //	GUILayout.Label("对象路径", GUILayout.Width(WIDTH));
        //	GUILayout.EndHorizontal();
        //	//列表显示内容
        //	mScrollPos = GUILayout.BeginScrollView(mScrollPos, GUILayout.Width(WIDTH * 2 + 20));
        //	//列表显示内容
        //	for (int i = 0; i < mInActiveList.Count; i++) {
        //		GUILayout.BeginHorizontal();
        //		GUILayout.Label(mInActiveList[i].inActiveObject.name,GUILayout.Width(WIDTH));
        //		GUILayout.Label(mInActiveList[i].path);
        //		GUILayout.EndHorizontal();
        //	}
        //	GUILayout.EndScrollView();

        //	//提供清理方法
        //	if (GUILayout.Button("清空扫描记录")) {
        //		ClearResourceList();
        //	}
        //}
        #endregion
    }

    //添加资源
    const string DEFAULT_PATH = "No Path";
    private void AddResource(Object resource, string name, string type, string sceneName) {
        if (resource == null)
            return;
        //获取资源地址作为key值
        string path = AssetDatabase.GetAssetPath(resource);
        string key;
        if("".Equals(path)) {
            key = name;
            path = DEFAULT_PATH;
        } else {
            key = path;
        }
        if (mResourceDict.ContainsKey(key)) {
            ResourceHolder resourceHolder = mResourceDict[key];
            resourceHolder.ResourceUseCount++;
            //对于场景名，目前看来似乎没有需要全部都显示的必要，所以先不做了
        } else {
            string md5 = path.Equals(DEFAULT_PATH) ? "" : MD5(path);
            ResourceHolder resourceHolder = new ResourceHolder {
                ResourceObject = resource,
                ResourcePath = path,
                ResourceName = name,
                ResourceType = type,
                ResourceUseCount = 1,
                UseSceneName = sceneName,
                ResourceMd5 = md5
            };
            mResourceDict.Add(key, resourceHolder);
            mResourceList.Add(resourceHolder);
        }
    }
    
    //使用说明对话框
    private class IllustrationWindow : EditorWindow {
        const string ILLUSTRATION_TEXT = @"场景资源扫描器：
——————————————————准备工作————————————————————————————
注：如果只扫描当前场景，不用扫描文件中场景，那么不用做 准备1 和 准备3
准备1.将“项目场景文件夹路径”设为场景列表中场景所在的文件夹 
准备2.将“插件工作文件夹”目录设为需要输出结果的目录。
准备3.在“插件工作文件夹”目录下创建一个.csv文件，并一定要将其命名为sceneList.csv，然后在文件输入想要操作的场景名（一行一个）（注:场景名不要加.unity的后缀！只需要场景名即可）即可。

——————————————————资源查找功能—————————————————————————
按钮1 ——“扫描当前场景资源并保存”—— 查找当前场景下资源并保存到“插件工作文件夹”目录下 
按钮2 ——“扫描列表中所有场景资源（分别保存）”—— 扫描列表（插件工作文件夹/sceneList.csv）中的所有场景下的资源，然后每个场景保存为一个文件，到“插件工作文件夹”目录下

—————————————————未激活对象查找功能——————————————————————
按钮3 ——“扫描当前场景中未激活对象并保存”—— 查找当前场景中所有未激活的GameObject并保存到“插件工作文件夹”目录下
按钮4 ——“扫描列表中所有未激活对象（分别保存）”—— 扫描列表（插件工作文件夹/sceneList.csv）中的所有场景下的未激活的GameObject，然后每个场景保存为一个文件，到“插件工作文件夹”目录下";
        private void OnGUI() {
            GUILayout.TextArea(ILLUSTRATION_TEXT);
        }
    }

    #region 构建资源
    //构建资源列表
    private void BuildResourceList (Scene targetScene)
	{
		//拿到当前scene的所有GameObject
		GameObject[] sceneObjects = (GameObject[])targetScene.GetRootGameObjects ();

		//遍历当前场景所有GameObject
		for (int i = 0; i < sceneObjects.Length; i++) {

			//Renderer
			Renderer[] renderers = sceneObjects [i].GetComponentsInChildren<Renderer> (true);
			for (int j = 0; j < renderers.Length; j++) {
                //material
                Renderer renderer = PrefabUtility.GetPrefabParent(renderers[j]) as Renderer;
                if (renderer != null && renderer.sharedMaterials != null) {
					Material[] rendererMaterials = renderer.sharedMaterials;
					for (int k = 0; k < rendererMaterials.Length; k++) {
                        if(rendererMaterials[k] == null) {
                            Debug.Log("Missing material! objName:" + renderer.gameObject.name);
                        }
						AddResource (rendererMaterials [k], rendererMaterials [k].name, MATERIAL, targetScene.name);
						AddMaterialTextureResource (rendererMaterials [k], targetScene);
					}
				}
			}


			//AudioSource
			AudioSource[] audioSources = sceneObjects [i].GetComponentsInChildren<AudioSource> (true);
			for (int j = 0; j < audioSources.Length; j++) {
				//audioclip
				if (audioSources [j].clip != null) {
					AddResource (audioSources [j].clip, audioSources [j].clip.name, AUDIO_CLIP, targetScene.name);
				}
			}

			//Image
			Image[] images = sceneObjects [i].GetComponentsInChildren<Image> (true);
			for (int j = 0; j < images.Length; j++) {
				//sprite
				if (images [j].sprite != null) {
					AddResource (images [j].sprite, images [j].sprite.name, SPRITE, targetScene.name);
				}
                //material
                Image image = PrefabUtility.GetPrefabParent(images[j]) as Image;
                if (image != null && image.material != null) {
					AddResource (image.material, image.material.name, MATERIAL, targetScene.name);
					AddMaterialTextureResource (image.material, targetScene);
				}
			}

			//Text
			Text[] texts = sceneObjects [i].GetComponentsInChildren<Text> (true);
			for (int j = 0; j < texts.Length; j++) {
				//font
				if (texts [j].font != null) {
					AddResource (texts [j].font, texts [j].font.name, FONT, targetScene.name);
				}
				//material
				if (texts [j].material != null) {
					AddResource (texts [j].material, texts [j].material.name, MATERIAL, targetScene.name);
					AddMaterialTextureResource (texts [j].material, targetScene);
				}
			}

			//Mesh Collider
			MeshCollider[] meshColliders = sceneObjects [i].GetComponentsInChildren<MeshCollider> (true);
			for (int j = 0; j < meshColliders.Length; j++) {
				//mesh
				if (meshColliders [j].sharedMesh != null) {
					AddResource (meshColliders [j].sharedMesh, meshColliders [j].sharedMesh.name, MESH, targetScene.name);
				}
			}

			//Mesh Filter
			MeshFilter[] meshFilters = sceneObjects [i].GetComponentsInChildren<MeshFilter> (true);
			for (int j = 0; j < meshFilters.Length; j++) {
                //mesh
                MeshFilter meshFilter = PrefabUtility.GetPrefabParent(meshFilters[j]) as MeshFilter;
				if (meshFilter != null && meshFilter.sharedMesh != null) {
                    Mesh mesh = meshFilter.sharedMesh;
                    AddResource (mesh, mesh.name, MESH, targetScene.name);
				}
			}

			//Animation
			Animation[] animations = sceneObjects[i].GetComponentsInChildren<Animation>(true);
			for (int j = 0; j < animations.Length; j++) {
				//Animation Clip
				AddAnimationClipResource(animations[j], targetScene);
			}

			//Animator
			Animator[] animators = sceneObjects[i].GetComponentsInChildren<Animator>(true);
			for (int j = 0; j < animators.Length; j++) {
				//Runtime Animator Controller
				if (animators [j].runtimeAnimatorController != null) {
					AddResource (animators [j].runtimeAnimatorController, animators [j].runtimeAnimatorController.name,
						RUNTIME_ANIMATOR_CONTROLLER, targetScene.name);
				}
				//Avator
				if (animators [j].avatar != null) {
					AddResource (animators [j].avatar, animators [j].avatar.name, AVATOR, targetScene.name);
				}
			}

			//Prefab
			FindPrefabObject(sceneObjects[i],targetScene);
		}

	}

	//构建场景中未激活的gameobject列表
	private void BuildInActiveResourceList(Scene targetScene){
		GameObject[] allSceneGameObject = (GameObject[]) targetScene.GetRootGameObjects ();
		for (int i = 0; i < allSceneGameObject.Length; i++) {
			FindInActive (allSceneGameObject [i],allSceneGameObject [i].name);
		}
	}

	//构建资源文件夹中的所有资源列表
	private void BuildDirsResourceList(){
		List<string> dirs = new List<string> ();
		GetDirs (Application.dataPath, ref dirs);
	}
	private void GetDirs(string dirPath, ref List<string> dirs){
		foreach (string path in Directory.GetFiles(dirPath))
		{
			foreach (string postfix in ResourceExts) {
				//获取所有文件夹中包含后缀为 .mat 的路径
				if (System.IO.Path.GetExtension (path) == postfix) {
					dirs.Add (path.Substring (path.IndexOf ("Assets")));
					char[] pathChar = path.Substring (path.IndexOf ("Assets")).ToCharArray ();
					char[] resultChar = new char[pathChar.Length];
					System.Array.Reverse (pathChar);
					int i = 0;
					while (i < pathChar.Length && pathChar [i] != '\\') {
						resultChar [i] = pathChar [i];
						i++;
					}
					System.Array.Reverse (resultChar);
					StringBuilder builder = new StringBuilder ();
					for (int j = 0; j < resultChar.Length; j++) {
						if (resultChar [j] != '\0') {
							builder.Append (resultChar [j]);
						}
					}
					//Debug.Log (builder.ToString ().Split ('.') [0]);
					DirsResourceHolder holder = new DirsResourceHolder (){ 
						name = builder.ToString ().Split ('.') [0], 
						path = path.Substring (path.IndexOf ("Assets")).Replace('\\','/'), 
						postfix = postfix 
					};
					mDirsResourceList.Add (holder);

					//Debug.Log (holder.name + ' ' + holder.path);
					//Debug.Log(path.Substring(path.IndexOf("Assets")));
					//Debug.Log (new string(pathChar).Trim());
				}
			}
		}
		if (Directory.GetDirectories(dirPath).Length > 0)  //遍历所有文件夹
		{
			foreach (string path in Directory.GetDirectories(dirPath))
			{
				GetDirs(path, ref dirs);
			}
		}
	}

	//构建当前场景未使用的资源的列表
	private void BuildUnUsedResourceList(){
		mUnUsedResourceList.Clear ();
		BuildResourceList (EditorSceneManager.GetActiveScene());
		BuildDirsResourceList ();
		bool tmp;
		for (int i = 0; i < mDirsResourceList.Count; i++) {
			tmp = false;
			for (int j = 0; j < mResourceList.Count; j++) {
				if (mResourceList [j].ResourcePath == mDirsResourceList [i].path) {
					tmp = true;
					break;
				}
			}
			if (tmp == false) {
				mUnUsedResourceList.Add (mDirsResourceList [i]);
				Debug.Log ("游戏中没用到的资源" + mDirsResourceList [i].path);
			}

		}
	}

	#endregion

	#region 文件处理
	//保存资源到目标文件目录的文件，会对文件进行编号，对于已经存在的文件，会迭代编号创建新文件
	private bool SaveResToFile(string folderPath, string fileName) {
		string path = folderPath + fileName + "_resource_" + System.DateTime.Now.ToString("yy_MM_dd") + ".csv";
		using (StreamWriter textWriter = new StreamWriter(path, false, System.Text.Encoding.Default)) {
			string text = EncodeResourceList();
			textWriter.Write(text);
		}
		return true;
	}

	//直接保存资源到指定文件
	private bool SaveResToFile(string filePath) {
		Debug.Log(filePath);
		using (StreamWriter textWriter = new StreamWriter(filePath, false, System.Text.Encoding.Default)) {
			string text = EncodeResourceList();
			textWriter.Write(text);
		}
		return true;
	}

	//同时保存资源使用情况和对象激活情况
	private bool SaveObjToFile(string folderPath, string fileName) {
        string path = folderPath + fileName + "_object_" + System.DateTime.Now.ToString("yy_MM_dd") + ".csv";
		using (StreamWriter objTextWriter = new StreamWriter(path, false, System.Text.Encoding.Default)) {
			string objText = EncodeObjList();
			objTextWriter.Write(objText);
		}
		return true;
	}

	//从指定文件中加载内容
	private bool LoadFromFile(string filePath) {
		ClearResourceList();
		using (StreamReader textReader = File.OpenText(filePath)) {
			DecodeResourceList(textReader.ReadToEnd());
		}
		return true;
	}

    //数据编码解码
    private string EncodeResourceList() {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append("资源名称").Append(',')
            .Append("资源路径").Append(',')
            .Append("资源类型").Append(',')
            .Append("资源被使用场景").Append(',')
            .Append("资源使用次数").Append(',')
            .Append("资源md5").Append('\n');
        for (int i = 0; i < mResourceList.Count; i++) {
            stringBuilder.Append(mResourceList[i].ResourceName).Append(',')
                .Append(mResourceList[i].ResourcePath).Append(',')
                .Append(mResourceList[i].ResourceType).Append(',')
                .Append(mResourceList[i].UseSceneName).Append(',')
                .Append(mResourceList[i].ResourceUseCount).Append(',')
                .Append(mResourceList[i].ResourceMd5).Append('\n');
        }
        stringBuilder.Remove(stringBuilder.Length - 1, 1);
        return stringBuilder.ToString();
    }

    //暂时被弃用了，如要启用需要参照编码修改
    private void DecodeResourceList(string text) {
        string[] allResourceStrs = text.Split('\n');
        for (int i = 1; i < allResourceStrs.Length; i++) {
            string[] resourceStr = allResourceStrs[i].Split(',');
            if(resourceStr.Length == 4) {
                ResourceHolder resourceHolder = new ResourceHolder() {
                    ResourcePath = resourceStr[0],
                    ResourceName = resourceStr[1],
                    ResourceType = resourceStr[2],
                    ResourceUseCount = int.Parse(resourceStr[3])
                };
                mResourceDict.Add(resourceHolder.ResourcePath, resourceHolder);
                mResourceList.Add(resourceHolder);
            }
        }
    }

    private string EncodeObjList() {
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append("对象名称").Append(',')
            .Append("对象路径（场景内）").Append('\n');
        for (int i = 0; i < mInActiveList.Count; i++) {
            stringBuilder.Append(mInActiveList[i].inActiveObject.name).Append(',')
                .Append(mInActiveList[i].path).Append('\n');
        }
        stringBuilder.Remove(stringBuilder.Length - 1, 1);
        return stringBuilder.ToString();
    }
	#endregion

	#region 工具
	//根据资源类型排序
	private void SortWithType(bool sortOrder = true) {
		if(sortOrder)
			mResourceList.Sort((ResourceHolder resource1, ResourceHolder resource2) =>
				resource1.ResourceType.CompareTo(resource2.ResourceType)
			);
		else
			mResourceList.Sort((ResourceHolder resource1, ResourceHolder resource2) =>
				resource2.ResourceType.CompareTo(resource1.ResourceType)
			);
	}

	//根据资源使用次数排序
	private void SortWithUseCount(bool sortOrder = true) {
		if(sortOrder)
			mResourceList.Sort((ResourceHolder resource1, ResourceHolder resource2) =>
				resource1.ResourceUseCount - resource2.ResourceUseCount
			);
		else
			mResourceList.Sort((ResourceHolder resource1, ResourceHolder resource2) =>
				resource2.ResourceUseCount - resource1.ResourceUseCount
			);
	}

	//从文件中获取所有需要扫描的场景名字
	private string[] GetNameOfFilesToScan(string filePath) {
		string[] result = null;
        List<string> resultList = new List<string>();
		using (TextReader textReader = File.OpenText(filePath)) {
			result = textReader.ReadToEnd().Split('\n');
		}
        for(int i = 0; i < result.Length; i++) {
            if (result[i].Length > 0)
                resultList.Add(result[i]);
        }
		return resultList.ToArray();
	}

	//清理
	private void ClearResourceList() {
		mResourceDict.Clear();
		mResourceList.Clear();
		//mTypeSortStr = "资源类型";
		//mUseSortStr = "资源使用次数";
		//mUseSortOrder = true;
		//mTypeSortOrder = true;
		mInActiveList.Clear ();
	}

	private T1[] GetResourceFromComponent<T1>(Object mat) where T1 : Object{
		Object[] roots = new Object[]{ mat };
		Object[] dependObjs = EditorUtility.CollectDependencies (roots);
		List<T1> results = new List<T1> ();
		for (int i = 0; i < dependObjs.Length; i++) {
			if (dependObjs [i].GetType () == typeof(T1)) {
				results.Add ((T1)dependObjs [i]);
			}
		}
		return results.ToArray ();
	}

	private void AddMaterialTextureResource(Material mat, Scene targetScene){
		Texture2D[] textures = GetResourceFromComponent<Texture2D> (mat);
		if (textures != null) {
			for (int i = 0; i < textures.Length; i++) {
				//texture2d
				AddResource (textures [i], textures [i].name, TEXTURE2D, targetScene.name);
			}
		}
	}

	private void AddAnimationClipResource(Animation anim, Scene targetScene){
		AnimationClip[] clips = GetResourceFromComponent<AnimationClip> (anim);
		if (clips != null) {
			for (int i = 0; i < clips.Length; i++) {
				AddResource (clips [i], clips [i].name, ANIMATION_CLIP, targetScene.name);
			}
		}
	}

	private void FindInActive(GameObject root, string gameobjectPath){
		if (root.activeSelf == false) {
			mInActiveList.Add (new InActiveHolder (){ inActiveObject = root, path = gameobjectPath });
		} else if (root.transform.childCount != 0) {
			foreach (Transform child in root.transform) {
				FindInActive (child.gameObject, gameobjectPath + '/' + child.name);
			}
		}
	}

	private void FindPrefabObject(GameObject root, Scene targetScene){
		Object prefab = PrefabUtility.GetPrefabParent (root);
		if (prefab != null) {
			AddResource (prefab, prefab.name, PREFAB, targetScene.name);
			return;
		} else if (root.transform.childCount != 0) {
			foreach (Transform child in root.transform) {
				FindPrefabObject (child.gameObject, targetScene);
			}
		}
	}
	#endregion

    //MD5计算
    public static string MD5(string filePath) {
        try {
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            using (FileStream file = new FileStream(filePath, FileMode.Open)) {
                byte[] retVal = md5Provider.ComputeHash(file);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < retVal.Length; i++) {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        catch (System.Exception ex) {
            Debug.LogError("Compute md5 failed, file path: " + filePath + ", Exception massage: " + ex.Message);
            return "";
        }
    }
}
