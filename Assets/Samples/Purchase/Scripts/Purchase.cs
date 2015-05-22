using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System;
using LitJson_Gamedonia;


public class Purchase : MonoBehaviour {

	public Texture2D carImg;
	public Texture2D backgroundImg;
	public GUISkin skin;
	
	private string [] productsList = new string[]{"gas"};
	private string errorMsg = "";
	private string statusMsg = "";
	private string console = "";

	private float gasProgress = 0.5f;
	private const float GAS_DRIVE_CONSUMPTION_REFILL = 0.25f;


	void Awake() {

	}

	void Start() {

		if (  Gamedonia.INSTANCE == null) {
			
			statusMsg = "Missing Api Key/Secret. Check the README.txt for more info.";
			return;
		}

		GamedoniaUsers.Authenticate(OnLogin);
		printToConsole ("Starting session with Gamedonia...");

		//Register the callback
		GDInAppService reqService = new GDInAppService();
		reqService.RegisterEvent += new InAppEventHandler(OnProductsRequested);
		GamedoniaStoreInAppPurchases.AddRequestService(reqService);

		GDInAppService buyService = new GDInAppService();
		buyService.RegisterEvent += new InAppEventHandler(OnProductPurchased);
		GamedoniaStoreInAppPurchases.AddPurchaseService(buyService);
	}

	void OnGUI () {
		
		GUI.skin = skin;

		GUI.enabled = (statusMsg == "");

		GUI.DrawTexture(UtilResize.ResizeGUI(new Rect(0,0,320,480)),backgroundImg);

		//Draw the car image
		GUI.DrawTexture(UtilResize.ResizeGUI(new Rect(120,10,128,128)),carImg);

		//Debug.Log (Screen.width - (UtilResize.resMultiplier() * 50));
		GUI.Label(UtilResize.ResizeGUI(new Rect((320-50+80)/2,120,50,20)),"GAS","LabelBold");
		GUI.Box (UtilResize.ResizeGUI (new Rect (118, 138, 132, 18)), "");
		GUI.Box (UtilResize.ResizeGUI (new Rect (120, 140, (float)(128*gasProgress), 14)),"","ProgressForeground");



		//Drive Btn
		if (GUI.Button (UtilResize.ResizeGUI(new Rect (80,180, 220, 50)), "Drive")) {
			drive();
		}

		if (GUI.Button (UtilResize.ResizeGUI(new Rect (80,240, 220, 50)), "Buy Gas")) {
			purchaseGas();
		}

	
		//Text area control
		GUI.Label(UtilResize.ResizeGUI(new Rect(80,300,220,20)),"Console Log:","LabelBold");
		GUI.Box (UtilResize.ResizeGUI (new Rect (80, 320, 220, 150)), console);


		if (errorMsg != "") {
			GUI.Box (new Rect ((Screen.width - (UtilResize.resMultiplier() * 260)),(Screen.height - (UtilResize.resMultiplier() * 50)),(UtilResize.resMultiplier() * 260),(UtilResize.resMultiplier() * 50)), errorMsg);
			if(GUI.Button(new Rect (Screen.width - 20,Screen.height - UtilResize.resMultiplier() * 45,16,16), "x","ButtonSmall")) {
				errorMsg = "";
			}
		}

		GUI.enabled = true;
		if (statusMsg != "") {
			GUI.Box (UtilResize.ResizeGUI(new Rect (80, 240 - 40, 220, 40)), statusMsg,"BoxBlack");
		}
	}

	private void drive() {

		if (gasProgress <= 0) {
			printToConsole("Out of GAS! Purchase more please!");
		}else {
			updateGas(-GAS_DRIVE_CONSUMPTION_REFILL);
		}

	}

	void changeProgress(float newValue){
		//apply the value of newValue:
		this.gasProgress = newValue;
	}

	void OnLogin (bool success) {

		statusMsg = "";
		if (success) {
			printToConsole("Session started successfully. uid: " + GamedoniaUsers.me._id);	

			//Requesting products		
			GamedoniaStore.RequestProducts (productsList, productsList.Length);

		}else {
			errorMsg = Gamedonia.getLastError().ToString();
			Debug.Log(errorMsg);
		}

	}

	private void OnProductsRequested() {

		if (GamedoniaStore.productsRequestResponse.success) {

				foreach (KeyValuePair<string, Product> entry in GamedoniaStore.productsRequestResponse.products) {
						Product product = (Product)entry.Value;
						printToConsole ("Received Product: " + product.identifier + " price: " + product.priceLocale + " description: " + product.description);
				}
		} else {
			printToConsole("Unable to request products! message: " + GamedoniaStore.productsRequestResponse.message);
		}
	}

	private void purchaseGas() {

		if (!Application.isEditor) {
			if (gasProgress < 1) {
				statusMsg = "Purchasing GAS...";
				GamedoniaStore.BuyProduct ("gas");
			}else {
				printToConsole("Already full of gas! Drive to spend some.");
			}
		} else {
			errorMsg = "Purchase is disabled in Editor mode! Try the sample on a device.";
		}
	}

	private void updateGas(float value) {

		GameObject go = GameObject.FindWithTag ("MainCamera");
		//establish parameter hash:
		Hashtable ht = iTween.Hash("from",gasProgress,"to",gasProgress+value,"time",.5f,"onupdate","changeProgress");

		//make iTween call:
		if (!(iTween.Count (gameObject) > 0)) {
			iTween.ValueTo (go, ht);
		}
	}

	private void OnProductPurchased() {
		statusMsg = "";
		PurchaseResponse purchase = GamedoniaStore.purchaseResponse;
		string details = "Purchase Result status: " + purchase.status + " for product identifier: " + purchase.identifier;
		if (purchase.message != null && purchase.message.Length > 0)
						details += " message: " + purchase.message;
		printToConsole (details);

		if (purchase.success) {
			printToConsole("GAS refilled.");
			updateGas(GAS_DRIVE_CONSUMPTION_REFILL);
		}
	}

	private void printToConsole(string msg) {
		console += msg + "\n";
	}
}
