using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;

public class Test : MonoBehaviour {

	[MenuItem("Tools/bianli")]
	static void CheckSceneSetting(){
		List<string> dirs = new List<string> ();
		GetDirs (Application.dataPath, ref dirs);
	}

	private static void GetDirs(string dirPath, ref List<string> dirs){
		foreach (string path in Directory.GetFiles(dirPath))
		{
			//获取所有文件夹中包含后缀为 .mat 的路径
			if (System.IO.Path.GetExtension(path) == ".mat")
			{
				dirs.Add(path.Substring(path.IndexOf("Assets")));
				char[] pathChar = path.Substring(path.IndexOf("Assets")).ToCharArray ();
				char[] resultChar = new char[pathChar.Length];
				Array.Reverse (pathChar);
				int i = 0;
				while (i < pathChar.Length && pathChar [i] != '\\') {
					resultChar [i] = pathChar [i];
					i++;
				}
				Array.Reverse (resultChar);
				StringBuilder builder = new StringBuilder ();
				for (int j = 0; j < resultChar.Length; j++) {
					if (resultChar [j] != '\0') {
						builder.Append (resultChar [j]);
					}
				}
				Debug.Log (builder);
				//Debug.Log(path.Substring(path.IndexOf("Assets")));
				//Debug.Log (new string(pathChar).Trim());
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
}
