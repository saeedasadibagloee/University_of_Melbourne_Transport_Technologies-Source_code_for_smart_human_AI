using UnityEngine;

namespace Assets.Scripts
{
    public class AudioController : MonoBehaviour
    {

        private readonly string[] _secretKeyword = { "d", "a", "r", "t", "h" };
        private int _letter = 0;
        private AudioSource _audio;

        void Start()
        {
            _audio = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.anyKeyDown)
            {
                if (_letter > _secretKeyword.Length)
                    _letter = 0;

                if (Input.GetKeyDown(_secretKeyword[_letter]))
                {
                    _letter++;
                }
                else
                {
                    _letter = 0;
                }
            }

            if (_letter == _secretKeyword.Length)
            {
                _letter = 0;

                if (_audio.isPlaying)
                {
                    _audio.Pause();
                }
                else
                {
                    _audio.Play();
                }
            }
        }
    }
}
