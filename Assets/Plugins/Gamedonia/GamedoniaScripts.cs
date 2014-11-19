using UnityEngine;
using System;
using System.Collections.Generic;
using LitJson_Gamedonia;
using MiniJSON_Gamedonia;

public class GamedoniaScripts  {

	public GamedoniaScripts () {		
	}
	
	public static void Run(string script, Dictionary<string,object> parameters, Action<bool, object> callback = null) {
		
		string json = JsonMapper.ToJson(parameters);
		
		Gamedonia.RunCoroutine(
			GamedoniaRequest.post("/run/" + script, json, null, GamedoniaUsers.GetSessionToken(), null,
				delegate (bool success, object data) {								
					if (callback!=null) {
						if (success) {
							callback(success,Json.Deserialize((string)data));
						}else {
							callback(success,null);
						}							
					}
				}
		 	 )
		);
	}
}
