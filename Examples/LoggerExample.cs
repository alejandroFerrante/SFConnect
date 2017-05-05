using UnityEngine;
using System.Collections;
using SalesforceConnect;//NOTICE THIS

/*
	This simple script will subscribe to the SalesforceSession object and
	register any event in the Debug Log.
	The part where sfController is set and logged in is covered in other example.
*/
public class LoggerExample : MonoBehaviour {

	public SalesforceSession sfController;//Reference via editor the gameObject that holds the SalesforceSession script
	private int sfID;
	
	void Start () {
		sfID = sfController.Subscribe(transform);
		Debug.Log("Logger Subscribed to SalesforceSession object with Id: "+sfID);
	}
	
	public void OnEventHandle(string message){
		Debug.Log("Event Reported: "+message);
	}
	
}
