using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConwaysGameOfLife : MonoBehaviour
{
    public Texture input;               // initial condition

    public int width = 512;             // size of texture...maybe?
    public int height = 512;

    public ComputeShader compute;
    public RenderTexture renderTexPing;
    public RenderTexture renderTexPong;

    public Material material;

    private int kernel;
    private bool pingPong;

    // Use this for initialization
    void Start()
    {
        if (height < 1 || width < 1) return;
        kernel = compute.FindKernel("GameOfLife");

        renderTexPing = new RenderTexture(width, height, 24);
        renderTexPing.wrapMode = TextureWrapMode.Repeat;        // this is supposed to make a "torus" (top-bot and l-r texture wrapping)... doesn't work tho
        renderTexPing.enableRandomWrite = true;
        renderTexPing.filterMode = FilterMode.Point;
        renderTexPing.useMipMap = false;
        renderTexPing.Create();

        renderTexPong = new RenderTexture(width, height, 24);
        renderTexPong.wrapMode = TextureWrapMode.Repeat;
        renderTexPong.enableRandomWrite = true;
        renderTexPong.filterMode = FilterMode.Point;
        renderTexPong.useMipMap = false;
        renderTexPong.Create();

        Graphics.Blit(input, renderTexPing);

        pingPong = true;

        compute.SetFloat("Width", width);
        compute.SetFloat("Height", height);

        float[] f = golRule();
        //float[] f = randomRule(0.125f); // testing
        compute.SetFloats("f", f); // passing the rule array to the shader
    }
    
    float[] randomRule(float p){
        float[] ret = new float[512 * 4];
        for (int i = 0; i < 512 * 4; i += 4)
        {
            ret[i] = Random.Range(0,1f) > p ? 0 : 1;
        }
        return ret;
    }

    float[] golRule(){
        float[] ret = new float[512 * 4];

        // step through all possible 512 neighborhood patters
        for (int i = 0; i < 512 * 4; i += 4)
        {
            string binary = intToBin(i/4);
            int n = 0;

            // count alive cells in current neighborhood pattern
            for(int k = 0; k < binary.Length; k++){
                n += binary[k] == '1' ? 1 : 0;
            }

            /*
            the rule array (ret) carries the indicies of the state alphabet

            example:

            ret[127] = 0
            
            this refers to the neighborhood pattern of 001111111 (127 in binary)
            or written in 2D:
            001
            111
            111

            this neighborhood pattern gets assigned the next state of s[0]
            in the case of the GoL, this is the state 0, or "false"
            the middle cell is represented by the 5th digit of the 9bit binary number
            */
            bool alive = binary[4] == '1';
            if(alive){
                if(n == 3 || n == 4){ // 2 or 3 neighbors alive --> cell lives on
                    ret[i] = 1f;
                } else {
                    ret[i] = 0f;
                }
            }else{
                if(n == 3){
                    ret[i] = 1f;
                } else{
                    ret[i] = 0f;
                }
            }
        }
        return ret;
    }

    // converts an integer to a binary number
    string intToBin(int i){
        int remainder;
        string ret = "";
        while (i > 0)
        {
            remainder = i % 2;
            i /= 2;
            ret = remainder.ToString() + ret;
        }
        ret = ret.PadLeft(9, '0');
        return ret;
    }

    // Update is called once per frame
    void Update()
    {
        if (height < 1 || width < 1) return;

        if (true == pingPong)
        {
            compute.SetTexture(kernel, "Input", renderTexPing);
            compute.SetTexture(kernel, "Result", renderTexPong);
            compute.Dispatch(kernel, width / 8, height / 8, 1);
            material.mainTexture = renderTexPong;
            pingPong = false;
        }
        else
        {
            compute.SetTexture(kernel, "Input", renderTexPong);
            compute.SetTexture(kernel, "Result", renderTexPing);
            compute.Dispatch(kernel, width / 8, height / 8, 1);
            material.mainTexture = renderTexPing;
            pingPong = true;
        }
    }
}