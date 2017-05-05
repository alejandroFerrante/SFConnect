using UnityEngine;
using System;
using System.Collections;
using System.Text;
using Boomlagoon.JSON;
using System.Collections.Generic;
/*
	PRÉLIMINAIRES
	
Before you go any further, you need to know that a basic understanding of what Unity3D and Salesforce is (if not, why would you want to comunicate these unknown tings?).
Furthermore, you should be able to create a brand new org and create a conected App. From this app you should be able to extract from it your client id,
client secret and security token. 


	OVERVIEW


Salesforce-Connect is a simple and minimalistic library by Alejandro Ferrante that handles basic communication with a Salesforce org and your code in Unity 3d.

Salesforce-Connect includes only two objects: SalesforcSession and SalesforceQueryResult.
As you can imagine, SalesforcSession represents an interface between your target org and your C# or UniyScript code.
The typical steps are to configure, login and then operate over the session. (see Examples section).

Salesforce is a little bit different from regular relational databases. Instead of thinking in terms of tables, rows, columns, etc. you interact with salesforce objects (sobjects).
Once you have a logged session, you can ask for all records of a certain type, or if you know the specific ids of the records you want, you can ask only for them. 
Objects are represented with Dictionaries with string keys and string values (field:value).

Every Query will create a SalesforceQueryResult object. You communicate with it as you would do with a list of sobjects (again, represented by a dictionary of string field:value)
The status of the session will be described by the status variables: State, last error and last error message. They are public variables you can inspect any time.

*/
namespace SalesforceConnect{
	
	public partial class SalesforceSession : MonoBehaviour {
		
		//public Status Variables
			public STATE currentState = STATE.CREATED;
			public ERROR lastError;
			public string lastErrorMessage;
		
		//public Status Values	
			public enum ERROR {NONE,LOGIN_ERROR, CALL_ERROR}
			public enum STATE {CREATED,LOGGED,WAITING}
			
			static  string EVENT_CONFIG 				= "CONFIGURATON WAS CHANGED";
			static  string EVENT_LOGIN_SUCCESSFUL 		= "LOGIN SUCCESSFUL";
			static  string EVENT_LOGIN_UNSUCCESSFUL 	= "LOGIN FAILED";
			static  string EVENT_INSERT_REQUESTED 		= "INSERT REQUESTED";
			static  string EVENT_UPDATE_REQUESTED 		= "UPDATE REQUESTED";
			static  string EVENT_DELETE_REQUESTED 		= "DELETE REQUESTED";
			static  string EVENT_QUERY_SUCCESSFUL 		= "QUERY SUCCESSFUL";
			static  string EVENT_QUERY_UNSUCCESSFUL 	= "QUERY UNSUCCESSFUL";
			static  string EVENT_QUERY_ERROR 			= "QUERY ERROR";
			static  string EVENT_CRUD_SUCCESSFUL 		= "CRUD SUCCESSFUL";
			static  string EVENT_CRUD_UNSUCCESSFUL 		= "CRUD UNSUCCESSFUL";
			static  string EVENT_CRUD_ERROR 			= "CRUD ERROR";
			
		/*
			**CONFIG**
			
			Before Login, you must set salesforce org specific configuration variables: 
			auth endpoint, org client secret, org client id, sf user security token, 
			grant type (typically 'password', don't change it unless you know what you are doing),
			api version and a report activated flag. (see Reporting section).
			NOTE: You will not be able to login unless all these variables are correctly set.
			
		*/
			public void setConfiguration(string authEndpoint, string orgClientSecret, string orgClientId, string securityToken, string sfGrantType, string sfVersion, bool reportAvtivated){
				oAuthEndpoint = authEndpoint;
				clientSecret = orgClientSecret; 
				clientId = orgClientId; 
				personalSecurityToken = securityToken; 
				grantType = sfGrantType; 
				version  = sfVersion;
				REPORT_ON = reportAvtivated;
				
				reportEvent(SalesforceSession.EVENT_CONFIG);
			}
			
		/*
			**LOGIN**
			
				Assuming all your variables are correctly set, Login will communicate with the target Salesforce org you have set and obtain a token.
				This token is necessary for any interaction. The token, however, will remain private for anyone outside this class.
				Interaction with Salesforce are always made through a web service via http. The Login request is made asynchronously. 
				Login may take a couple of seconds. State will remain as "WAITING" until the response arrives.
		*/
			public void Login(string username, string password){
				
				if ((currentState == STATE.CREATED) && (loginDataIsComplete())){
					currentState = STATE.WAITING;
					WWWForm form = new WWWForm();

					form.AddField("username", username);
					form.AddField("password", password);
					form.AddField("client_secret", clientSecret);
					form.AddField("client_id", clientId);
					form.AddField("grant_type", grantType);
					WWW result = new WWW(oAuthEndpoint, form);
					
					StartCoroutine(setToken(result));
					reportEvent("LOGIN REQUESTED");
				}
			}
		
		/*
				Check that session is logged
		*/
			public bool isLogged(){
				return (currentState == STATE.LOGGED);
			}
		
		/*
			**QUERY**
			
				QUERY ALL
			
				By specifying the sobject type and the desired fields, you can query for all available records of that certain sobject.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
				'deletePreviousQueryResult' flag indicates whether or not previous query should be deleted or not.
		*/
			public void QueryForAllRecordsOfType(string objectType, List<string> fields, bool deletePreviousQueryResult){
				if (isLogged()){
					
					string query = "SELECT";
					foreach(string field in fields){
						query += " "+field+",";
					}
					query = query.Substring(0,query.Length - 1);
					query += " FROM "+objectType;
					
					string url = instanceUrl + "/services/data/" + version + "/query?q=" + WWW.EscapeURL(query);

					WWWForm form = new WWWForm();			
					System.Collections.Generic.Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Content-Type"] = "application/json";
					headers["Method"] = "GET";
					WWW www = new WWW(url, null, headers);
					
					if(deletePreviousQueryResult){GameObject.Destroy(lastQueryResult);}
					lastQueryResult = new GameObject();
					lastQueryResult.name = "Query Result";
					lastQueryResult.AddComponent<SalesforceQueryResult>();
					lastQueryResult.GetComponent<SalesforceQueryResult>().setType(objectType);
					
					request(www,CALL_TYPE.QUERY);
					reportEvent("QUERY REQUESTED");
					
				}
			}
		
		/*
				QUERY SPECIFIC RECORDS
			
				You can query for specific records by providing their ids, sobject type, and desired fields.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
				'deletePreviousQueryResult' flag indicates whether or not previous query should be deleted or not.
		*/
			public void QueryForRecords(string objectType, List<string> fields, List<string> ids, bool deletePreviousQueryResult ){
				if (isLogged()){
					
					string query = "SELECT";
					foreach(string field in fields){
						query += " "+field+",";
					}
					query = query.Substring(0,query.Length - 1);
					query += " FROM "+objectType+" WHERE Id IN ( ";
					foreach(string id in ids){
						query += " '"+id+"',";
					}
					query = query.Substring(0,query.Length - 1);
					query +=" )";
					
					string url = instanceUrl + "/services/data/" + version + "/query?q=" + WWW.EscapeURL(query);

					WWWForm form = new WWWForm();			
					System.Collections.Generic.Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Content-Type"] = "application/json";
					headers["Method"] = "GET";
					WWW www = new WWW(url, null, headers);
					
					if(deletePreviousQueryResult){GameObject.Destroy(lastQueryResult);}
					lastQueryResult = new GameObject();
					lastQueryResult.name = "Query Result";
					lastQueryResult.AddComponent<SalesforceQueryResult>();
					lastQueryResult.GetComponent<SalesforceQueryResult>().setType(objectType);
					
					request(www,CALL_TYPE.QUERY);
					reportEvent("QUERY REQUESTED");
					
				}
			}		

		/*
				GENERIC QUERY
			
				A more advanced feature: 
				By providing a sobject type and a string query you can perform a generic SOQL query.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
				'deletePreviousQueryResult' flag indicates whether or not previous query should be deleted or not.
		*/
			public void Query(string q, string objectType, bool deletePreviousQueryResult){
				if (isLogged()){
					string url = instanceUrl + "/services/data/" + version + "/query?q=" + WWW.EscapeURL(q);

					WWWForm form = new WWWForm();			
					System.Collections.Generic.Dictionary<string,string> headers = form.headers;
					headers["Authorization"] = "Bearer " + token;
					headers["Content-Type"] = "application/json";
					headers["Method"] = "GET";
					WWW www = new WWW(url, null, headers);
					
					if(deletePreviousQueryResult){GameObject.Destroy(lastQueryResult);}
					lastQueryResult = new GameObject();
					lastQueryResult.name = "Query Result";
					lastQueryResult.AddComponent<SalesforceQueryResult>();
					lastQueryResult.GetComponent<SalesforceQueryResult>().setType(objectType);
					
					request(www,CALL_TYPE.QUERY);
					
				}
			}
			
		/*
			**QUERY RESULT**
			
				Check that session has a query result
		*/
			public bool hasQuery(){
				return (isLogged() && (lastQueryResult != null));
			}
			
		/*
				Get last query result
		*/	
			public SalesforceQueryResult getLastQueryResult(){
				if(lastQueryResult != null){
					return lastQueryResult.GetComponent<SalesforceQueryResult>();
				}else{
					return null;
				}
			}
			
		/*
				EXTRACT RECORD FROM QUERY RESULT
			
				Once there's an actual query result, you can obtain the record by providing its id.
		*/
			public Dictionary<string,string> GetRecordFromResult(string id){
				if(hasQuery()){
					return lastQueryResult.GetComponent<SalesforceQueryResult>().GetRecordMapById(id);
				}
				return null;
			}
			
		/*
			**CRUD OPERATIONS**
			
				INSERT RECORD
				
				By providing sobject type and an object representation (Dictionary of string field:value) you will be able to create a new record.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
				(record will be assigned an id once it is inserted)
		*/
			public void insert(string objectType, Dictionary<string,string> objectToInsert){
				string requestBody = "{";
				
				foreach(string key in objectToInsert.Keys){
					if(key != "id" && key != "Id"){
						requestBody+= "\""+key+"\" : \""+objectToInsert[key]+"\",";
					}
				}
				
				requestBody = requestBody.Substring(0, requestBody.Length - 1);
				requestBody += "}";
				
				GameObject.Destroy(lastQueryResult);
				
				insert(objectType,requestBody);
				
			}
			
		/*
				UPDATE EXISTING RECORD
				
				By providing sobject type and an object representation (Dictionary of string field:value) you will be able to update an existing record.
				NOTE: It is mandatory that the object has the id as one of is fields. Oherwise, operation will not be requested.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
		*/
			public void update(string objectType, Dictionary<string,string> objectToInsert){
				if(objectToInsert.ContainsKey("id")){
					string requestBody = "{";
					
					foreach(string key in objectToInsert.Keys){
						if(key != "id" && key != "Id"){
							requestBody+= "\""+key+"\" : \""+objectToInsert[key]+"\",";
						}
					}
					
					requestBody = requestBody.Substring(0, requestBody.Length - 1);
					requestBody += "}";
					
					GameObject.Destroy(lastQueryResult);
					
					update(objectToInsert["id"],objectType,requestBody);
				}
			}
		
		/*
				DELETE EXISTING RECORD
				
				By providing sobject type and an object representation (Dictionary of string field:value) you will be able to delete an existing record.
				NOTE: It is mandatory that the object has the id as one of is fields. Oherwise, operation will not be requested.
				The result will be a SalesforceQueryResult. The request is done asynchronously, so you will have to wait for the response to arrive. (See Reporting Section)
		*/
			public void delete(string objectType, Dictionary<string,string> objectToInsert){
				if(objectToInsert.ContainsKey("id")){
									
					GameObject.Destroy(lastQueryResult);
					
					delete(objectToInsert["id"],objectType);
				}
			}
			
		/*
			**REPORTING**
			
				As you can see, almost all operations that interact with Salesforce is done asynchronously via web service. This eans you perform a equest 
				and then have to wait for a response from the other side. But how do you know when this response actually arrives? You can never know for sure. 
				Wouldn't be great if someone just notified you? What about other events? Enter: Reporting.
				
				Reporting is the way you can stay synched with the SalesforceSession object. You can activated when configuring the session with setConfiguration,
				or later on with ActivateReports or DeactivateReports. You can also checked if the flag is activated with ReportsActivated.
				
				If reporting activated, all asynchronic operations will produce report (a predefined string message) on completion and send it to all session's subscribers. 
				This is the fundamental mechanism in which you will design your interactions.
				VERY IMPORTANT: All objects you wish to subscribe must implement a method called 'OnEventHandle' with the following signature:

					public void OnEventHandle(string message){
						<YOUR CODE HERE>
					}
				
				You may attach more than one script that implements this on one gameObject, but if you wish to subscribe it, it must have at least one that implements it.
				
				The typical use is: You have your subscriber. You configure your session in order to report events. You subscribe your subscriber. Done!
				Now your subscriber will receive notifications of any event performed by the SalesforceSession.
				
				Browse the examples to achieve a better understanding of the SalesforceSession.
			
				NOTE: Remember to browse the SalesforceQueryResult class to see how it works.
					
		*/
			public bool ReportsActivated(){
				return REPORT_ON;
			}
			
			public void ActivateReports(){
				REPORT_ON = true;
			}
			
			public void DeactivateReports(){
				REPORT_ON = false;
			}
			
			public int Subscribe(Transform subscriber){
				if(subscribers == null){subscribers = new Dictionary<int,Transform>();}
				
				subscribers[lastId] = subscriber;
				lastId++;
				return lastId-1;
				
			}
			
			public void Unsubscribe(int subscriberId){
				subscribers.Remove(subscriberId);
			}
		/*
		**FINALLY...**
						
				Salesforce-Connect is completely open source library. This means you are encouraged to modify it as you please.
				If you consider that you can produce a better version or even add functionality to it, go ahead!
				Both classes have a "private" part where the behind the scenes action takes place. Again, it's rather simple.
				Go to these files for a complete understanding of the implementation.
		*/
	}
}
