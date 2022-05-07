using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class CameraToggle : MonoBehaviour
{
     
    public GameObject[] cams;
     
    int currentCam;
     
    // Start is called before the first frame update
    void Start()
    {
        currentCam = 0;
        setCam(currentCam);
    }

    public void setCam(int idx){
        for(int i = 0; i < cams.Length; i++){
            if(i == idx){
                cams[i].SetActive(true);
            }else{
                cams[i].SetActive(false);
            }
        }
    }
     
    public void toggleCam(){
        currentCam++;
        if(currentCam > cams.Length-1)
            currentCam = 0;
        setCam(currentCam);
    }
}