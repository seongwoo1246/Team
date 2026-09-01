package com.yourcompany.auth;

import android.app.Activity;
import android.os.Bundle;
import android.os.CancellationSignal;
import android.util.Log;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;

import com.google.android.libraries.identity.googleid.GetGoogleIdOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.unity3d.player.UnityPlayer;

import java.util.concurrent.Executors;

public class CredentialManagerHelper {
    private static final String TAG = "CredentialManagerHelper";

    public static void requestGoogleLogin(Activity activity, String webClientId, String callbackObjectName, String callbackMethodName) {
        CredentialManager credentialManager = CredentialManager.create(activity);

        GetGoogleIdOption googleIdOption = new GetGoogleIdOption.Builder()
                .setFilterByAuthorizedAccounts(false)
                .setServerClientId(webClientId)
                .setAutoSelectEnabled(false)
                .build();

        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(googleIdOption)
                .build();

        CancellationSignal cancellationSignal = new CancellationSignal();

        credentialManager.getCredentialAsync(
                activity,
                request,
                cancellationSignal,
                Executors.newSingleThreadExecutor(),
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override
                    public void onResult(GetCredentialResponse result) {
                        Credential credential = result.getCredential();
                        if (credential instanceof CustomCredential &&
                                credential.getType().equals(GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL)) {
                            try {
                                GoogleIdTokenCredential googleIdTokenCredential = GoogleIdTokenCredential.createFrom(credential.getData());
                                String idToken = googleIdTokenCredential.getIdToken();
                                UnityPlayer.UnitySendMessage(callbackObjectName, callbackMethodName, idToken);
                            } catch (Exception e) {
                                Log.e(TAG, "Error parsing Google ID Token", e);
                                UnityPlayer.UnitySendMessage(callbackObjectName, callbackMethodName, "ERROR: Token parsing failed");
                            }
                        } else {
                            UnityPlayer.UnitySendMessage(callbackObjectName, callbackMethodName, "ERROR: Invalid Credential Type");
                        }
                    }

                    @Override
                    public void onError(GetCredentialException e) {
                        Log.e(TAG, "Credential Manager Error", e);
                        UnityPlayer.UnitySendMessage(callbackObjectName, callbackMethodName, "ERROR: " + e.getMessage());
                    }
                }
        );
    }
}