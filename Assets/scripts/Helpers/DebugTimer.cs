using System;
using UnityEngine;
using System.Diagnostics;

public class DebugTimer: IDisposable
{
	Stopwatch watch;
	string logString;
	public DebugTimer (string str)
	{
		logString = str;
		watch = new Stopwatch();
		watch.Start();
	}
	public void Dispose()
	{
		watch.Stop ();
		UnityEngine.Debug.Log(string.Format("{0}: {1}", logString, watch.Elapsed));
	}
}

