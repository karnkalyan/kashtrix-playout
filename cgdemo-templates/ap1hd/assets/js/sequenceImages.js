function playImageSequenceAnimation(
  canvas, 
  imageFolder, 
  frameCount, 
  fps, 
  fileNameTemplate, 
  extension = '.png', 
  leadingZeros = 5, 
  startIndex = 0, 
  loop = false,           // Not used for restarting now; we always restart after finish.
  holdLastFrame = false, 
  onComplete = null, 
  triggerFrame = null,    // Frame at which to trigger the ticker/subrange
  onTrigger = null,       // Function to execute at trigger frame
  frameRange = null,      // Array [startFrame, endFrame] for subrange loop
  completeMainSequence = true  // If false, jump to subrange immediately at triggerFrame
) {
  const ctx = canvas.getContext('2d');
  const images = [];
  let currentFrame = 0;
  let isPlaying = true;
  let subrangeTimerId = null;
  let imageLoadedCount = 0;
  // Phase can be "main_before_subrange", "subrange", or "main_after_subrange"
  let phase = "main_before_subrange";

  // Load images
  for (let i = 0; i < frameCount; i++) {
    const img = new Image();
    const paddedIndex = (startIndex + i).toString().padStart(leadingZeros, '0');
    const fileName = fileNameTemplate.replace("{index}", paddedIndex);
    img.src = `${imageFolder}${fileName}${extension}`;

    img.onload = () => {
      imageLoadedCount++;
      if (imageLoadedCount === 1) {
        canvas.width = img.width;
        canvas.height = img.height;
      }
      if (imageLoadedCount === frameCount) {
        playMainSequence();
      }
    };

    images.push(img);
  }

  // Main sequence function for both "before" and "after" subrange
  function playMainSequence() {
    if (!isPlaying) return;

    // Run if we're in one of the main phases.
    if (phase === "main_before_subrange" || phase === "main_after_subrange") {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(images[currentFrame], 0, 0, canvas.width, canvas.height);

      // In the "main_before_subrange" phase, if we've reached the trigger frame,
      // fire the onTrigger callback and switch to subrange (if completeMainSequence is false)
      if (
        phase === "main_before_subrange" &&
        triggerFrame !== null &&
        currentFrame >= triggerFrame &&
        !completeMainSequence &&
        frameRange &&
        Array.isArray(frameRange) &&
        frameRange.length === 2
      ) {
        if (onTrigger && typeof onTrigger === 'function') {
          onTrigger();
        }
        phase = "subrange";
        playSubRangeAnimation();
        return;
      }

      // If we've reached the end of the main sequence, trigger onComplete then restart from the beginning.
      if (currentFrame >= frameCount - 1) {
        if (onComplete && typeof onComplete === 'function') {
          onComplete();
        }
        // Always restart from the beginning.
        phase = "main_before_subrange";
        currentFrame = 0;
      } else {
        currentFrame++;
      }
    }

    setTimeout(playMainSequence, 1000 / fps);
  }

  // Subrange loop: continuously loop between specified frames.
  function playSubRangeAnimation() {
    let subFrame = frameRange[0];
    function subLoop() {
      // If phase has changed (ticker completed), exit the subrange loop.
      if (phase !== "subrange") return;

      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.drawImage(images[subFrame], 0, 0, canvas.width, canvas.height);

      if (subFrame < frameRange[1]) {
        subFrame++;
      } else {
        subFrame = frameRange[0];
      }
      subrangeTimerId = setTimeout(subLoop, 1000 / fps);
    }
    subLoop();
  }

  // This function is meant to be called by your ticker's onTickerComplete callback.
  // It cancels the subrange loop and resumes the main sequence from the frame after the subrange.
  function resumeMainSequence() {
    if (subrangeTimerId) {
      clearTimeout(subrangeTimerId);
      subrangeTimerId = null;
    }
    // Switch phase to "main_after_subrange" and resume from the frame immediately after the subrange.
    if (frameRange && Array.isArray(frameRange) && frameRange.length === 2) {
      phase = "main_after_subrange";
      currentFrame = frameRange[1] + 1;
      if (currentFrame >= frameCount) {
        // In case subrange ends at the very end, reset to start.
        phase = "main_before_subrange";
        currentFrame = 0;
      }
    } else {
      phase = "main_before_subrange";
    }
    playMainSequence();
  }

  // Expose resumeMainSequence so your ticker can call it.
  canvas.resumeMainSequence = resumeMainSequence;

  // Optional: toggle play/pause on click.
  canvas.addEventListener('click', () => {
    isPlaying = !isPlaying;
    if (isPlaying) {
      if (phase !== "subrange") {
        playMainSequence();
      }
    }
  });
}
