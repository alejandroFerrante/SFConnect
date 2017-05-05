using UnityEngine;
using System;
using System.Collections;
using System.Text;
using Boomlagoon.JSON;
using System.Collections.Generic;
namespace SalesforceConnect{
	
	public partial class SalesforceSession : MonoBehaviour {
		
			//CONFIGURATION VARIABLES
			private string oAuthEndpoint;
			private string clientSecret;
			private string clientId;
			private string personalSecurityToken; 
			private string grantType = "password";
			private string version;
			private bool REPORT_ON = false;
			
			//EVENT REPORT VARIABLES
			private Dictionary<int,Transform> subscribers;//Dictionary that holds all subscribers
			private int lastId = 0;//id counter
			
			//INTERNAL VARIABLES
			private GameObject lastQueryResult;//reference to the gameObject that holds the SalesforceQueryResult coponent
			private string rawResponse;//raw JSON query response
			
			private string token;//token provided by Salesforce on successful login.
			private string instanceUrl;
			
			private enum CALL_TYPE {QUERY, CRUD}
			
			void Start(){
				if(subscribers == null){subscribers = new Dictionary<int,Transform>();}
			}
			
			//LOGIN COROUTINE
			IEnumerator setToken(WWW www) {
				yield return www;//wait until HTP response arrives
				if (www.error == null){
						if(!www.text.Contains("error")){//Login successful
						
						// parse JSON Response
							var obj = JSONObject.Parse(www.text);
						
						// extract token and instance url
							token = obj.GetString("access_token");
							instanceUrl = obj.GetString("instance_url");
						
						// set corresponding state
							currentState = STATE.LOGGED;
							lastError = ERROR.NONE;
							lastErrorMessage = "";
						
						// report
							reportEvent(SalesforceSession.EVENT_LOGIN_SUCCESSFUL);
						
					}else{//request returned with error
						
						// set corresponding state
							lastError = ERROR.LOGIN_ERROR;
							currentState = STATE.CREATED;
						
						//fill error message
							lastErrorMessage = extractErrorMessageFromJSON("error_description",www.text);
						
						// report
							reportEvent(SalesforceSession.EVENT_LOGIN_UNSUCCESSFUL);
						
					}
				} else {//error sending request
								
					// set corresponding state
						lastError = ERROR.LOGIN_ERROR;
						currentState = STATE.CREATED;
					
					//fill error message
						lastErrorMessage = www.error.ToString();
					
					// report
						reportEvent(SalesforceSession.EVENT_LOGIN_UNSUCCESSFUL);
					
				}   
			}
			
			// REQUEST FOR QUERY OR CRUD OPERATION
			private void request(WWW www,CALL_TYPE callType){
				
				//set state to waiting until response is received
				rawResponse = null;
				currentState = STATE.WAITING;
				
				//forward call to corresponding method
				switch(callType){
					case CALL_TYPE.QUERY:
						StartCoroutine(executeQueryCall(www));
						break;
					
					case CALL_TYPE.CRUD:
						StartCoroutine(executeCRUDCall(www));
						break;
				}
				
			}
			
			//QUERY COROUTINE
			IEnumerator executeQueryCall(WWW www){
				yield return www;//wait(sleep) until HTTP response arrives
				
				if (www.error == null){
					if(!www.text.Contains("errorCode")){//Returned without error
						
						//fill response
							rawResponse = www.text;
						
						//set corresponding state
							lastError = ERROR.NONE;
							lastErrorMessage = "";
						
						//complete SalesforceQueryResult creation with JSON response
							lastQueryResult.GetComponent<SalesforceQueryResult>().FillWith(rawResponse);
						
						//report
							reportEvent(SalesforceSession.EVENT_QUERY_SUCCESSFUL);
						
					}else{//Returned with error
						
						//fill response
							rawResponse = www.text;
							
						//set corresponding state
							lastError = ERROR.CALL_ERROR;
						
						//fill error message
							lastErrorMessage = extractErrorMessageFromJSON("message",www.text);

						//report
						reportEvent(SalesforceSession.EVENT_QUERY_UNSUCCESSFUL+lastErrorMessage);
						
					}
				} else {//Could not be sent
					
					//set corresponding state
						rawResponse = "";
						lastError = ERROR.CALL_ERROR;
						lastErrorMessage = www.error;
					
					//resport
						reportEvent(SalesforceSession.EVENT_QUERY_ERROR+lastErrorMessage);
				}
				currentState = STATE.LOGGED;//no matter the result of the query, sate must return to normal
				
			}
			
			//CRUD COROUTINE
			IEnumerator executeCRUDCall(WWW www){
				yield return www;//wait(sleep) until HTP response arrives
				
				if (www.error == null){
					if(!www.text.Contains("errorCode")){//Returned without error
						
						//fill response
							rawResponse = www.text;
						
						//set corresponding state
							lastError = ERROR.NONE;
							lastErrorMessage = "";
						
						//report
							reportEvent(SalesforceSession.EVENT_CRUD_SUCCESSFUL);
							
					}else{//Returned with error
					
						//fill response
							rawResponse = www.text;
							
						//set corresponding state
							lastError = ERROR.CALL_ERROR;
							lastErrorMessage = extractErrorMessageFromJSON("message",www.text);

						//report
							reportEvent(SalesforceSession.EVENT_CRUD_UNSUCCESSFUL+lastErrorMessage);
					}
				} else {//Could not be sent
				
					//set corresponding state
						rawResponse = "";
						lastError = ERROR.CALL_ERROR;
						lastErrorMessage = www.error;
					
					//report
						reportEvent(SalesforceSession.EVENT_CRUD_ERROR+lastErrorMessage);
				}
				currentState = STATE.LOGGED;//no matter the result of the query, sate must return to normal
				
			}

			
			
			private void insert(string sobject, string body){
				//construct url
					string url = instanceUrl + "/services/data/" + version + "/sobjects/" + sobject;
				//construct form
					WWWForm form = new WWWForm();			
					Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Content-Type"] = "application/json";
					headers["Method"] = "POST";
				
				//construct WWW object with url and form
					WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);
				
				//do request
					request(www,CALL_TYPE.CRUD);
				
				//report
					reportEvent(SalesforceSession.EVENT_INSERT_REQUESTED);
			}
			
			private void update(string id, string sobject, string body){
				
				//consruct url
					string url = instanceUrl + "/services/data/" + version + "/sobjects/" + sobject + "/" + id + "?_HttpMethod=PATCH";
				//consruct form
					WWWForm form = new WWWForm();			
					Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Content-Type"] = "application/json";
					headers["Method"] = "POST";
					
				//construct WWW object with url and form
					WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

				//do request
					request(www,CALL_TYPE.CRUD);
				
				//report
				reportEvent(SalesforceSession.EVENT_UPDATE_REQUESTED);
			}
			
			private void delete(string id, string sobject){
				
				//construct url
					string url = instanceUrl + "/services/data/" + version + "/sobjects/" + sobject + "/" + id + "?_HttpMethod=DELETE";
					
				//construct form
					WWWForm form = new WWWForm();			
					Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Method"] = "POST";
					// need something in the body for DELETE to work for some reason
					String body = "DELETE";
				
				//costruct WWW object with url and form
					WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);
				
				//do request
					request(www,CALL_TYPE.CRUD);
				
				//report
					reportEvent(SalesforceSession.EVENT_DELETE_REQUESTED);
			}
			
			
			/*
				This method is used to extract a given error message from a JSON
			*/
			private string extractErrorMessageFromJSON(string errorKey,string sourceJSON){
				string result = "";
				if(sourceJSON.Contains(errorKey)){
					int lowerIndex = sourceJSON.IndexOf(errorKey) + errorKey.Length + 3; // 3 = length of '":"'
					int upperIndex = 0;
					while(sourceJSON[upperIndex+lowerIndex] != '"'){
						upperIndex++;
					}
					result = sourceJSON.Substring(lowerIndex,upperIndex);
				}
				return result;
			}
			
			/*
				Check if all configuration data is complete.
				NOTE: Data filled does not necessarily mean data is correct!
			*/
			private bool loginDataIsComplete(){
				return (oAuthEndpoint != "") && (clientSecret != "") && (clientId != "") && (personalSecurityToken != "") && (grantType != "") && (version != "") &&(oAuthEndpoint != null) &&(clientSecret != null) &&(clientId != null) &&(personalSecurityToken != null) &&(grantType != null) &&(version != null);
			}
			
			/*
				Invokes Handler method for every subscriber if reporting flag is activated
			*/
			private void reportEvent(string message){
				if(REPORT_ON){
					foreach(Transform subscriber in subscribers.Values){
						subscriber.SendMessage("OnEventHandle",message);
					}
				}
			}	
		
	}
}