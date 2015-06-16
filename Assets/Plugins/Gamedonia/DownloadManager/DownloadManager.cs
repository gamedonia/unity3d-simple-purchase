// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MiniJSON_Gamedonia;
using LitJson_Gamedonia;
using HTTP;

public class DownloadManager: DownloadDelegate
{

	public  const string DOWNLOAD_DID_FINISH_LOADING = "downloadDidFinishLoading";
	public  const string DOWNLOAD_DID_FAIL = "downloadDidFail";
	public  const string DOWNLOAD_DID_RECEIVE_DATA = "downloadDidReceiveData";
	public  const string DOWNLOAD_DID_START = "downloadDidStart";
	public  const string DOWNLOADMANAGER_DID_FINISH_LOADING_ALL = "downloadDidFinishLoadingAll";
	public  const string DOWNLOADMANAGER_DID_START_LOADING_ALL = "downloadDidStartLoadingAllForManager";


	private ArrayList _downloads;
	private ArrayList _downloadeds;
	private int _maxConcurrentDownloads;
	private bool _cancelAllInProgress;
	private bool _paused = false;
	private bool _masterPaused = false;
	private DownloadManagerDelegate downloadManagerDelegate;
	private string filesystemPath;

	public event DownloadManagerEventHandler DownloadDidFinishLoading;
	public event DownloadManagerEventHandler DownloadDidFail;
	public event DownloadManagerEventHandler DownloadDidReceiveData;
	public event DownloadManagerEventHandler DownloadDidStart;
	public event DownloadManagerEventHandler DownloadDidFinishLoadingAll;
	public event DownloadManagerEventHandler DownloadDidStartLoadingAllForManager;


	public DownloadManager (string filesystemPath)
	{
		_downloads = new ArrayList();
		_downloadeds = new ArrayList();
		_maxConcurrentDownloads = 4;
		this.filesystemPath = filesystemPath;


		Directory.CreateDirectory (filesystemPath);
	}

	public Download addDownloadWithFilename(string filename, string url, string fileId) {
		//Debug.Log("[Gamedonia DownloadManager] addDownloadWithFilename("+filename+","+url+","+fileId+"){ }");
		Download download = this.FindDownloadByFileId(fileId);
		if (download == null) {

			download = new Download();
			download.init(filename,url,fileId);
			download.DownloadDelegate = this;
			download.filesystemPath = this.filesystemPath;
			_downloads.Add(download);
		}
		

		//TODO Gestion de PAUSE
		//this.addEventListener(Event.ACTIVATE,onActivate);
		//this.addEventListener(Event.DEACTIVATE,onDeactivate);
		
		return download;
		
	}


	public void Start(bool resume = false) {
		
		if (resume) {
			_masterPaused = false;
			_paused = false;
			foreach (Download download in _downloads) 
			{
				download.Resume = true;
				download.ExcludeUntilResume = false;	
			}
		}
		
		this.tryDownloading();
	}

	public void CancellAll() {
		this._cancelAllInProgress = true;
		
		while (this._downloads.Count > 0) {
			((Download)this._downloads[0]).Cancel();
		}
		
		this._cancelAllInProgress = false;
		this.informDelegateThatDownloadsAreDone();
	}

	public void PauseAll() {
		
		InternalPauseAll();
		_masterPaused = true;
		
	}

	public void Activate() {

		if (_masterPaused) return;
		if (_paused) {
			this.Start(true);
			_paused = false;
		}
	}

	public void Deactivate() {

		//Debug.Log ("Deactivate");
		if (!_paused) {
			this.InternalPauseAll();
			_paused = true;
		}
	}

	private void InternalPauseAll() {

		//Debug.Log ("Internal Pause All");
		this._cancelAllInProgress = true;
		Download download = null;
		bool somethingPaused = false;
		for (int i = 0;i<this._downloads.Count;i++) {
			//Debug.Log ("Pausando...");
			download = (this._downloads[i] as Download);
			if (!somethingPaused && download.isDownloading()) somethingPaused = true; 
			(this._downloads[i] as Download).Pause();
		}			
		
		this._cancelAllInProgress = false;
		if (somethingPaused) this.informDelegateThatDownloadsAreDone();
		
	}

	private Download FindDownloadByFileId(string fileId) {
		
		foreach (Download download in _downloads) 
		{
			if (download.FileId.Equals(fileId)) return download;	
		}
		
		return null;
	}





	#region DownloadDelegate implementation
	public void downloadDidFinishLoading (Download download)
	{
			
		this.RemoveDownload(download);
		this.RemoveResolveDownloadUrl(JsonMapper.ToJson(download));
		this.dispatchEvent(DOWNLOAD_DID_FINISH_LOADING,download);

		#if UNITY_STANDALONE
		#else
		this.tryDownloading ();
		#endif


	}
	public void downloadDidFail (Download download)
	{
				
		this.RemoveDownload(download);
		this.RemoveResolveDownloadUrl(JsonMapper.ToJson(download));

		DownloadManagerEvent dEvent = new DownloadManagerEvent(this,download,DownloadManagerEvent.ERROR_DOWNLOAD_DID_FAIL);
		if (DownloadDidFail != null) DownloadDidFail(this, dEvent);
		
		if (!this._cancelAllInProgress) {
			#if UNITY_STANDALONE
			#else
			this.tryDownloading ();
			#endif
		}	
	}
	public void downloadDidFailConnectivity (Download download)
	{

		this.dispatchEvent (DOWNLOAD_DID_FAIL,download);

		DownloadManagerEvent dEvent = new DownloadManagerEvent(this,download,DownloadManagerEvent.ERROR_DOWNLOAD_DID_FAIL_CONNECTIVITY);
		if (DownloadDidFail != null) DownloadDidFail(this, dEvent);
		
		if (!this._cancelAllInProgress) {
			#if UNITY_STANDALONE
			#else
			this.tryDownloading ();
			#endif
		}		
	}
	public void downloadDidFailNoAvailableSpace (Download download)
	{
		this.RemoveDownload(download);
		this.RemoveResolveDownloadUrl(JsonMapper.ToJson(download));
		
		DownloadManagerEvent dEvent = new DownloadManagerEvent(this,download,DownloadManagerEvent.ERROR_DOWNLOAD_DID_FAIL_NO_SPACE);
		if (DownloadDidFail != null) DownloadDidFail(this, dEvent);
		
		if (!this._cancelAllInProgress) {
			#if UNITY_STANDALONE
			#else
			this.tryDownloading ();
			#endif
		}	
	}
	public void downloadDidReceiveData (Download download)
	{
		this.dispatchEvent (DOWNLOAD_DID_RECEIVE_DATA,download);
	}
	public void downloadDidStart (Download download)
	{

		this.dispatchEvent (DOWNLOAD_DID_START, download);
		
		bool allRunning = true;
		
		foreach (Download _download in _downloads) 
		{
			if (!_download.isDownloading() && !_downloadeds.Contains(_download)) {
				allRunning = false;
				break;
			}
		}
		
		if (allRunning) {
			this.dispatchEvent(DOWNLOADMANAGER_DID_START_LOADING_ALL,null);
		}
	}
	public void downloadDidPause (Download download)
	{
								
		if (!this._cancelAllInProgress) {
			this.tryDownloading();
		}		
	}
	#endregion
	

	public void RemoveDownload(Download download) {

		#if UNITY_STANDALONE
		if (_downloadeds == null) _downloadeds = new ArrayList();
		_downloadeds.Add(download);
		#else
		int removeIndex = -1;
		foreach (Download item in _downloads) 
		{
			if (item.FileId.Equals(download.FileId)) removeIndex = _downloads.IndexOf(item);
			
		}
		
		if (removeIndex != -1) _downloads.RemoveAt (removeIndex);
		#endif

	}
	
	public void RemoveResolveDownloadUrl(string resolvedDownloadUrl) {

		string resolvedDownloadUrlsSO = PlayerPrefs.GetString ("resolvedDownloadUrls");
		ArrayList resolvedDownloadUrls = JsonMapper.ToObject<ArrayList> (resolvedDownloadUrlsSO);
		IDictionary resolvedDownload = Json.Deserialize (resolvedDownloadUrl) as IDictionary;

		if (resolvedDownloadUrls != null) {
			int removeIndex = -1;
			foreach (string downloadJSON in resolvedDownloadUrls) {
					IDictionary download = Json.Deserialize(downloadJSON) as IDictionary;
					if (download ["FileId"].Equals (resolvedDownload ["FileId"])) {
							removeIndex = resolvedDownloadUrls.IndexOf (downloadJSON);
					}

			}

			if (removeIndex != -1) {
					resolvedDownloadUrls.RemoveAt (removeIndex);
			}

			PlayerPrefs.SetString ("resolvedDownloadUrls", JsonMapper.ToJson (resolvedDownloadUrls));
			PlayerPrefs.Save ();
		}
	}

	private void dispatchEvent(string type, Download download = null) {

		DownloadManagerEvent e = new DownloadManagerEvent(this,download);
		switch (type) {

			case DOWNLOAD_DID_FINISH_LOADING:					
					if (DownloadDidFinishLoading != null) DownloadDidFinishLoading(this,e);								
					break;
			case DOWNLOAD_DID_FAIL:					
					if (DownloadDidFail != null) DownloadDidFail(this,e);													
					break;
			case DOWNLOAD_DID_RECEIVE_DATA:
					if (DownloadDidReceiveData != null) DownloadDidReceiveData(this,e);					
					break;
			case DOWNLOAD_DID_START:
					if (DownloadDidStart != null) DownloadDidStart(this,e);					
					break;
			case DOWNLOADMANAGER_DID_FINISH_LOADING_ALL:
					if (DownloadDidFinishLoadingAll != null) DownloadDidFinishLoadingAll(this,e);					
					break;
			case DOWNLOADMANAGER_DID_START_LOADING_ALL:
					if (DownloadDidStartLoadingAllForManager != null) DownloadDidStartLoadingAllForManager(this,e);					
					break;

		}
			
	}

	private void tryDownloading() {
		int totalDownloads = this.getPendingDownloads();

		if (totalDownloads == 0) {
			this.informDelegateThatDownloadsAreDone();
			return;
		}
			
		if (this.countUnstartedDownloads() > 0  && this.countActiveDownloads() < this._maxConcurrentDownloads) {
				
			foreach (Download download in _downloads) 
			{
				if (!download.isDownloading()) {
					download.Start();
				}
			}

			#if UNITY_STANDALONE
			//Remove downloadeds
			ArrayList tmpDownloads = (ArrayList) _downloads.Clone();
			_downloads.Clear();
			foreach (Download download in tmpDownloads) {
				if (!_downloadeds.Contains(download)) _downloads.Add(download);
			}
			_downloadeds.Clear();
			
			this.tryDownloading();
			#endif
				
		}

	}
	
	private int getPendingDownloads() {
		
		int pendingDownloads = 0;
		foreach (Download download in _downloads) 
		{
			if (!download.ExcludeUntilResume) pendingDownloads++;
		}
		
		return pendingDownloads;
		
	}

	private void informDelegateThatDownloadsAreDone() {
		this.dispatchEvent (DOWNLOADMANAGER_DID_FINISH_LOADING_ALL, null);
	}

	private int countUnstartedDownloads() {
		
		int pendingDownloads = this.getPendingDownloads();			
		int unstartedDownloads = pendingDownloads - this.countActiveDownloads();
		if (unstartedDownloads < 0) unstartedDownloads = 0;
		
		return unstartedDownloads;
		
	}

	private int countActiveDownloads() {
		
		int activeDownloadCount = 0;
		
		foreach (Download download in _downloads) 
		{
			if (download.isDownloading()) activeDownloadCount++;
		}
		
		return activeDownloadCount;
	}

	public int maxConcurrentDownloads {
		get {
			return _maxConcurrentDownloads;
		}
		set {
			_maxConcurrentDownloads = value;
		}
	}
}

// Delegate declaration. 
public delegate void DownloadManagerEventHandler(object sender, DownloadManagerEvent e);

public class DownloadManagerEvent: EventArgs {

	public const int ERROR_DOWNLOAD_DID_FAIL = 0;
	public const int ERROR_DOWNLOAD_DID_FAIL_CONNECTIVITY = 1;	
	public const int ERROR_DOWNLOAD_DID_FAIL_NO_SPACE = 3;


	private Download download;
	private DownloadManager downloadManager;
	private int errorCode;

	public DownloadManagerEvent(DownloadManager downloadManager, Download download) {

		this.download = download;
		this.downloadManager = downloadManager;

	}

	public DownloadManagerEvent(DownloadManager downloadManager, Download download, int errorCode) {
		
		this.download = download;
		this.downloadManager = downloadManager;
		this.errorCode = errorCode;
		
	}

	public Download Download {
		get {
			return download;
		}
	}

	public DownloadManager DownloadManager {
		get {
			return downloadManager;
		}
	}
}

