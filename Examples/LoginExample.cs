using UnityEngine;
using System.Collections;
using SalesforceConnect;//NOTICE THIS

/*
	This simple script exposes the login with hadcoded values.
	Note that in any real case is a good idea to have information so important such as passwords
	or security tokens harcoded and unincripted!
*/
public class LoginExample : MonoBehaviour {
	
	public SalesforceSession sfController;//Reference via editor the gameObject that holds the SalesforceSession script
	
	public string endpoint = "https://login.salesforce.com/services/oauth2/token";
	public 	string clientSecret;//< HARDCODE YOUR CLIENT SECRET HERE >
	public string clientId;//< HARDCODE YOUR CLIENT ID HERE >
	public string securityToken;// <HARDCODE HERE YOUR SECURITY TOKEN>
	public string grantType = "password";
	public string version = "v29.0";
	
	public string username;//<HARCODE HERE YOUR USERNAME INCLUDING @>
	public string password;//< HARDCODE HERE YOUR PASSWORD >
	
	public void DoConfig(){
		sfController.setConfiguration(endpoint, clientSecret, clientId, securityToken , grantType, version, false);
	}
	
	public void DoLogin(){
		sfController.Login(username,password);
	}
}
