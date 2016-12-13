function [CVLabels] = getCVLabels(model)
rng(13, 'twister');

CVModel = crossval(model, 'KFold', 10);
CVLabels = kfoldPredict(CVModel);
end
