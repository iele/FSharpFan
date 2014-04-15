open Hammock
open Hammock.Authentication.OAuth
open System.Threading
open System.Collections.Generic

type FanfouAPI (dummy) =
    let mutable oauthToken:string = ""
    let mutable oauthSecret:string = ""
   
    let LoginSuccessEvent = new Event<_>()

    [<CLIEvent>]
    member this.LoginSuccess = LoginSuccessEvent.Publish

    member this.OauthToken
       with get() = oauthToken
       and set(value) = oauthToken <- value
    member this.OauthSecret
       with get() = oauthSecret
       and set(value) = oauthSecret <- value
  
    member this.GetClient() =
        new Hammock.RestClient(
            Authority = "http://api.fanfou.com",
            Credentials = new Hammock.Authentication.OAuth.OAuthCredentials (
                        Type = OAuthType.ProtectedResource,
                        ConsumerKey = "105927463df3dbf466ba5195f8f64a95",
                        ConsumerSecret = "6686029b7224a53c9a9bf0bdc3d6ce7f",
                        SignatureMethod = Hammock.Authentication.OAuth.OAuthSignatureMethod.HmacSha1,
                        ParameterHandling = Hammock.Authentication.OAuth.OAuthParameterHandling.HttpAuthorizationHeader,
                        Version = "1.0",
                        Token = oauthToken,
                        TokenSecret = oauthSecret))

    member this.Login(username:string, password:string):unit = 
        let credentials = 
            new Hammock.Authentication.OAuth.OAuthCredentials(
                ConsumerKey = "105927463df3dbf466ba5195f8f64a95",
                ConsumerSecret ="6686029b7224a53c9a9bf0bdc3d6ce7f", 
                SignatureMethod = Hammock.Authentication.OAuth.OAuthSignatureMethod.HmacSha1, 
                ParameterHandling = Hammock.Authentication.OAuth.OAuthParameterHandling.HttpAuthorizationHeader,  
                Version = "1.0")
        let client = 
            new RestClient(Authority="http://fanfou.com", Credentials=credentials )
        client.AddHeader("content-type", "application/x-www-form-urlencoded")
        let restRequest = 
            new Hammock.RestRequest(
                Path = "oauth/access_token",
                Method = new System.Nullable<Web.WebMethod>(Hammock.Web.WebMethod.Post))

        restRequest.AddParameter("x_auth_mode", "client_auth")
        restRequest.AddParameter("x_auth_username", username)
        restRequest.AddParameter("x_auth_password", password)

        client.BeginRequest (request = restRequest, callback =
            fun (requset:RestRequest) (response:RestResponse) (userstate) ->
               let content = response.Content.Split([|'='; '&'|])
               this.OauthToken <- content.[1]
               this.OauthSecret <- content.[3]
               LoginSuccessEvent.Trigger(this)
               ()
        ) |> ignore

    member this.StatusUpdate(status:string):unit = 
        let client = this.GetClient()
        client.AddHeader("content-type", "application/x-www-form-urlencoded")
        let restRequest = 
            new Hammock.RestRequest(
                Path = "statuses/update.json",
                Method = new System.Nullable<Web.WebMethod>(Hammock.Web.WebMethod.Post))
        restRequest.AddParameter("status", status)
        client.BeginRequest (request = restRequest, callback =
            fun (requset:RestRequest) (response:RestResponse) (userstate) ->              
                printfn "%s" response.Content
                exit(0)
        ) |> ignore  
                 
[<EntryPoint>]
let main argv = 
    if argv.Length <> 3 then
        printfn "Parameter Error"
        exit(2)


    let api = new FanfouAPI()
    api.Login(argv.[0], argv.[1])
    api.LoginSuccess.Add(fun (this)-> api.StatusUpdate(argv.[2]))

    while true do
        Thread.Sleep 1000   
    0