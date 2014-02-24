using UnityEngine;
using System.Diagnostics;

public class DebugTimer
{
	Stopwatch watch;
	string logString;
	public DebugTimer (string str)
	{
		logString = str;
		watch = new Stopwatch();
		watch.Start();
	}
	public void Stop()
	{
		watch.Stop ();
		UnityEngine.Debug.Log(string.Format("{0}: {1}", logString, watch.Elapsed));
	}
}

